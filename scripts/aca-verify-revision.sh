#!/usr/bin/env bash
# =============================================================================
# ProyectoCafe - Espera una revisión saludable de Azure Container Apps y hace
# un smoke test HTTP.
# =============================================================================
# Uso:
#   ./scripts/aca-verify-revision.sh \
#     --app pc-api \
#     --resource-group rg-proyectocafe-prod \
#     --image myacr.azurecr.io/pc-api:abc1234 \
#     --label API \
#     --path /health \
#     [--url https://api.example.com] \
#     [--timeout 600]
#
# Si no se pasa --url, se usa el FQDN del ingress externo de la app. Las apps
# con ingress interno requieren --url con una dirección alcanzable desde el
# runner (por ejemplo, un runner self-hosted dentro de la VNet).
# =============================================================================
set -euo pipefail

usage() {
    cat <<'EOF'
Uso: aca-verify-revision.sh --app NAME --resource-group RG --image IMAGE \
       --label LABEL [--path /health] [--url URL] [--timeout 600]

  --app             Nombre de la Container App.
  --resource-group  Resource group de la Container App.
  --image           Imagen completa (loginServer/repositorio:tag) de la revisión.
  --label           Etiqueta para los mensajes (por ejemplo API o Portal).
  --path            Ruta del smoke test (por defecto /health).
  --url             Base URL alcanzable desde el runner. Si falta, se usa el
                    FQDN del ingress externo de la app.
  --timeout         Segundos máximos de espera de la revisión (por defecto 600).
EOF
}

app=""
resource_group=""
image=""
label=""
path="/health"
url=""
timeout=600

while [ "$#" -gt 0 ]; do
    case "$1" in
        --app) app="${2:-}"; shift 2 ;;
        --resource-group) resource_group="${2:-}"; shift 2 ;;
        --image) image="${2:-}"; shift 2 ;;
        --label) label="${2:-}"; shift 2 ;;
        --path) path="${2:-}"; shift 2 ;;
        --url) url="${2:-}"; shift 2 ;;
        --timeout) timeout="${2:-}"; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *)
            echo "Argumento desconocido: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
done

for required in app resource_group image label; do
    if [ -z "${!required}" ]; then
        echo "Falta --${required//_/-}." >&2
        usage >&2
        exit 2
    fi
done

if ! [[ "$timeout" =~ ^[0-9]+$ ]] || [ "$timeout" -le 0 ]; then
    echo "--timeout debe ser un entero positivo." >&2
    exit 2
fi

case "$path" in
    /*) ;;
    *) path="/$path" ;;
esac

for dependency in az curl; do
    if ! command -v "$dependency" >/dev/null 2>&1; then
        echo "Falta la dependencia '$dependency' en el runner." >&2
        exit 2
    fi
done

echo "Esperando revisión saludable de $label ($app) con imagen $image..."
elapsed=0
last_state=""
query_error_printed=0

while [ "$elapsed" -lt "$timeout" ]; do
    row=""
    if ! row="$(az containerapp revision list \
        --name "$app" \
        --resource-group "$resource_group" \
        --query "[?contains(properties.template.containers[].image, '$image')] | sort_by(@, &properties.createdTime) | [-1].[name, properties.provisioningState, properties.runningState, properties.healthState, properties.active, properties.trafficWeight]" \
        --output tsv 2>&1)"; then
        if [ "$query_error_printed" -eq 0 ]; then
            echo "No se pudo consultar las revisiones de $app:" >&2
            printf '%s\n' "$row" >&2
            query_error_printed=1
        fi
        echo "Reintentando en 10s..."
        sleep 10
        elapsed=$((elapsed + 10))
        continue
    fi

    if [ -z "$row" ]; then
        echo "Todavía no existe una revisión con la imagen $image. Reintentando en 10s..."
        sleep 10
        elapsed=$((elapsed + 10))
        continue
    fi

    IFS=$'\t' read -r revision provisioning running health active traffic <<< "$row"
    state="revision=${revision:-?} provisioning=${provisioning:-?} running=${running:-?} health=${health:-?} active=${active:-?} traffic=${traffic:-?}"
    if [ "$state" != "$last_state" ]; then
        echo "$state"
        last_state="$state"
    fi

    traffic_ok=0
    if [[ "${traffic:-}" =~ ^[0-9]+$ ]] && [ "$traffic" -gt 0 ]; then
        traffic_ok=1
    fi

    if { [ "$provisioning" = "Provisioned" ] || [ "$provisioning" = "Succeeded" ]; } \
        && [ "$running" = "Running" ] \
        && [ "$health" = "Healthy" ] \
        && { [ "$active" = "true" ] || [ "$active" = "True" ]; } \
        && [ "$traffic_ok" -eq 1 ]; then
        echo "Revisión $revision saludable, activa y con tráfico."
        break
    fi

    if [ "$provisioning" = "Failed" ] || [ "$running" = "Failed" ] || [ "$health" = "Unhealthy" ]; then
        echo "La revisión $revision no está saludable ($state)." >&2
        az containerapp logs show \
            --name "$app" \
            --resource-group "$resource_group" \
            --revision "$revision" \
            --tail 100 || true
        exit 1
    fi

    sleep 10
    elapsed=$((elapsed + 10))
done

if [ "$elapsed" -ge "$timeout" ]; then
    echo "Timeout de ${timeout}s esperando una revisión saludable de $app." >&2
    exit 1
fi

resolve_url() {
    local explicit="${1:-}"
    if [ -n "$explicit" ]; then
        printf '%s' "${explicit%/}"
        return 0
    fi

    local fqdn external
    if ! fqdn="$(az containerapp show \
        --name "$app" \
        --resource-group "$resource_group" \
        --query 'properties.configuration.ingress.fqdn' \
        --output tsv)"; then
        echo "No se pudo obtener el FQDN de $app." >&2
        return 1
    fi
    if ! external="$(az containerapp show \
        --name "$app" \
        --resource-group "$resource_group" \
        --query 'properties.configuration.ingress.external' \
        --output tsv)"; then
        echo "No se pudo obtener el tipo de ingress de $app." >&2
        return 1
    fi

    if [ -z "$fqdn" ] || [ "$fqdn" = "None" ]; then
        echo "La app $app no tiene ingress con FQDN; define --url." >&2
        return 1
    fi
    if [ "$external" != "true" ] && [ "$external" != "True" ]; then
        echo "La app $app tiene ingress interno; define --url con una URL alcanzable desde el runner." >&2
        return 1
    fi

    printf 'https://%s' "$fqdn"
}

smoke_test() {
    local target="$1"
    local attempts=30
    local attempt code

    for attempt in $(seq 1 "$attempts"); do
        code="$(curl \
            --silent \
            --show-error \
            --location \
            --max-time 15 \
            --output /dev/null \
            --write-out '%{http_code}' \
            "$target" || true)"

        if [ "$code" = "200" ]; then
            echo "Smoke test OK: $target -> 200"
            return 0
        fi

        echo "Smoke test $label: intento $attempt/$attempts -> HTTP ${code:-sin respuesta}. Reintentando en 5s..."
        sleep 5
    done

    echo "Smoke test FALLIDO: $target no respondió 200 (último HTTP: ${code:-sin respuesta})." >&2
    return 1
}

base_url="$(resolve_url "$url")"
smoke_test "${base_url}${path}"
