// Solo el Admin O el OrderManager pueden entrar aquí
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTiendaConLogin.Data;
using MiTiendaConLogin.Models;

[Authorize(Roles = "Admin,OrderManager")]
public class OrdersController : Controller
{
    // ... tu lógica para ver órdenes ...
}