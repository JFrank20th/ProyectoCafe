using Microsoft.Extensions.DependencyInjection;
using ProyectoCafe.Application.Contabilidad.Costos;
using ProyectoCafe.Application.Contabilidad.Gastos;
using ProyectoCafe.Application.Contabilidad.Pasivos;
using ProyectoCafe.Application.Compras.Proveedores;
using ProyectoCafe.Application.Compras.RecepcionesCafe;
using ProyectoCafe.Application.Inventario.Insumos;
using ProyectoCafe.Application.Inventario.Lotes;
using ProyectoCafe.Application.Inventario.ProcesosInventario;
using ProyectoCafe.Application.Inventario.Productos;
using ProyectoCafe.Application.Inventario.UnidadesMedida;
using ProyectoCafe.Application.Negocio.Clientes;
using ProyectoCafe.Application.Negocio.MetodosPago;
using ProyectoCafe.Application.Negocio.TiposVenta;
using ProyectoCafe.Application.Negocio.Ventas;
using ProyectoCafe.Application.Produccion.Cultivos;
using ProyectoCafe.Application.Produccion.Fincas;
using ProyectoCafe.Application.Produccion.OperacionesAgricolas;

namespace ProyectoCafe.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICostosService, CostosService>();
        services.AddScoped<IGastosService, GastosService>();
        services.AddScoped<IPasivosService, PasivosService>();

        services.AddScoped<IUnidadesMedidaService, UnidadesMedidaService>();
        services.AddScoped<IProductosService, ProductosService>();
        services.AddScoped<ILotesService, LotesService>();
        services.AddScoped<IProcesosInventarioService, ProcesosInventarioService>();
        services.AddScoped<IInsumosService, InsumosService>();
        services.AddScoped<IFincasService, FincasService>();
        services.AddScoped<IOperacionesAgricolasService, OperacionesAgricolasService>();
        services.AddScoped<ICultivosService, CultivosService>();
        services.AddScoped<IProveedoresService, ProveedoresService>();
        services.AddScoped<IRecepcionesCafeService, RecepcionesCafeService>();
        services.AddScoped<IClientesService, ClientesService>();
        services.AddScoped<ITiposVentaService, TiposVentaService>();
        services.AddScoped<IVentasService, VentasService>();
        services.AddScoped<IMetodosPagoService, MetodosPagoService>();

        return services;
    }
}
