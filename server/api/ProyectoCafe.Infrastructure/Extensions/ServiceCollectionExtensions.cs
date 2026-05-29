using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProyectoCafe.Application.Contabilidad.Costos;
using ProyectoCafe.Application.Contabilidad.Gastos;
using ProyectoCafe.Application.Contabilidad.Pasivos;
using ProyectoCafe.Application.Compras.Proveedores;
using ProyectoCafe.Application.Compras.RecepcionesCafe;
using ProyectoCafe.Application.Inventario.Categorias;
using ProyectoCafe.Application.Inventario.Insumos;
using ProyectoCafe.Application.Inventario.Lotes;
using ProyectoCafe.Application.Inventario.Presentaciones;
using ProyectoCafe.Application.Inventario.Productos;
using ProyectoCafe.Application.Inventario.UnidadesMedida;
using ProyectoCafe.Application.Negocio.Clientes;
using ProyectoCafe.Application.Negocio.MetodosPago;
using ProyectoCafe.Application.Negocio.TiposVenta;
using ProyectoCafe.Application.Negocio.Ventas;
using ProyectoCafe.Application.Produccion.Fincas;
using ProyectoCafe.Application.Produccion.EmpaquesCafe;
using ProyectoCafe.Application.Produccion.OperacionesAgricolas;
using ProyectoCafe.Application.Produccion.Parcelas;
using ProyectoCafe.Application.Produccion.TransformacionesCafe;
using ProyectoCafe.Infrastructure.Configuration;
using ProyectoCafe.Infrastructure.Compras.Proveedores;
using ProyectoCafe.Infrastructure.Compras.RecepcionesCafe;
using ProyectoCafe.Infrastructure.Contabilidad.Costos;
using ProyectoCafe.Infrastructure.Contabilidad.Gastos;
using ProyectoCafe.Infrastructure.Contabilidad.Pasivos;
using ProyectoCafe.Infrastructure.Data;
using ProyectoCafe.Infrastructure.Inventario.Categorias;
using ProyectoCafe.Infrastructure.Inventario.Insumos;
using ProyectoCafe.Infrastructure.Inventario.Lotes;
using ProyectoCafe.Infrastructure.Inventario.Presentaciones;
using ProyectoCafe.Infrastructure.Inventario.Productos;
using ProyectoCafe.Infrastructure.Inventario.UnidadesMedida;
using ProyectoCafe.Infrastructure.Negocio.Clientes;
using ProyectoCafe.Infrastructure.Negocio.MetodosPago;
using ProyectoCafe.Infrastructure.Negocio.TiposVenta;
using ProyectoCafe.Infrastructure.Negocio.Ventas;
using ProyectoCafe.Infrastructure.Produccion.Fincas;
using ProyectoCafe.Infrastructure.Produccion.EmpaquesCafe;
using ProyectoCafe.Infrastructure.Produccion.OperacionesAgricolas;
using ProyectoCafe.Infrastructure.Produccion.Parcelas;
using ProyectoCafe.Infrastructure.Produccion.TransformacionesCafe;

namespace ProyectoCafe.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>();
        if (string.IsNullOrWhiteSpace(dbOptions?.ConnectionString))
        {
            throw new InvalidOperationException($"Missing required config: {DatabaseOptions.SectionName}:ConnectionString");
        }

        services.AddDbContext<ProyectoCafeDbContext>(options =>
            options.UseSqlServer(dbOptions.ConnectionString));

        services.AddScoped<ICostoRepository, CostoRepository>();
        services.AddScoped<IGastoRepository, GastoRepository>();
        services.AddScoped<IPasivoRepository, PasivoRepository>();

        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IUnidadMedidaRepository, UnidadMedidaRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IPresentacionRepository, PresentacionRepository>();
        services.AddScoped<ILoteRepository, LoteRepository>();
        services.AddScoped<IInsumoRepository, InsumoRepository>();
        services.AddScoped<IFincaRepository, FincaRepository>();
        services.AddScoped<IOperacionAgricolaRepository, OperacionAgricolaRepository>();
        services.AddScoped<IEmpaqueCafeRepository, EmpaqueCafeRepository>();
        services.AddScoped<IParcelaRepository, ParcelaRepository>();
        services.AddScoped<ITransformacionCafeRepository, TransformacionCafeRepository>();
        services.AddScoped<IProveedorRepository, ProveedorRepository>();
        services.AddScoped<IRecepcionCafeRepository, RecepcionCafeRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<ITipoVentaRepository, TipoVentaRepository>();
        services.AddScoped<IVentaRepository, VentaRepository>();
        services.AddScoped<IMetodoPagoRepository, MetodoPagoRepository>();

        return services;
    }
}
