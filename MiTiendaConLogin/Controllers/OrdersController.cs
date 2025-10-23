// Solo el Admin O el OrderManager pueden entrar aquí
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin,OrderManager")]
public class OrdersController : Controller
{
    // ... tu lógica para ver órdenes ...
}