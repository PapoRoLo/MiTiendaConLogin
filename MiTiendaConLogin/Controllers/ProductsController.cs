// Solo el Admin O el ProductManager pueden entrar aquí
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin,ProductManager")]
public class ProductsController : Controller
{
    // ... tu lógica para crear/editar productos ...
}