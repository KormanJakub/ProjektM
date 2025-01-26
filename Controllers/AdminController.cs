using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektM.Models;
using ProjektM.Services;

namespace ProjektM.Controllers;

[ApiController]
[Route("[controller]")]
public class AdminController : ControllerBase
{
    private readonly PmiContext _context;
    private readonly PasswordHasherService _hasherService;

    public AdminController(PmiContext context, PasswordHasherService hasherService)
    {
        _context = context;
        _hasherService = hasherService;
    }

    [HttpPost("add-product")]
    public async Task<IActionResult> AddProduct([FromBody] Product product)
    {
        if (product == null || string.IsNullOrWhiteSpace(product.Name))
            return BadRequest(new { Message = "Invalid product details." });
        
        var newProductId = 1; 
        if (await _context.Products.AnyAsync())
            newProductId = await _context.Products.MaxAsync(p => p.ProductId) + 1;

        product.ProductId = newProductId;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Product added successfully", ProductId = product.ProductId });
    }
    
    [HttpDelete("remove-product/{id}")]
    public async Task<IActionResult> RemoveProduct(int id)
    {
        var dbProduct = await _context.Products.FindAsync(id);
        if (dbProduct == null) 
            return NotFound(new { Message = "Product not found" });

        _context.Products.Remove(dbProduct);
        await _context.SaveChangesAsync();
        
        var response = new { Message = "Product removed successfully", ProductId = id };

        return Ok(response);
    }
    
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _context.Orders.Include(o => o.User).ToListAsync();
        return Ok(orders);
    }
    
    [HttpDelete("cancel-order/{id}")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound(new { Message = "Order not found" });

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Order cancelled successfully" });
    }
    
    [HttpPost("add-tag")]
    public async Task<IActionResult> AddTag([FromBody] Tag tag)
    {
        var newTagId = 1;
        if (await _context.Tags.AnyAsync())
            newTagId = await _context.Tags.MaxAsync(t => t.TagId) + 1;

        tag.TagId = newTagId;

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        
        return Ok(new { Message = "Tag added successfully"});
    }
    
    [HttpDelete("remove-tag/{id}")]
    public async Task<IActionResult> RemoveTag(int id)
    {
        var dbTag = await _context.Tags.FindAsync(id);
        if (dbTag == null)
            return NotFound(new { Message = "Tag not found" });

        foreach (var product in dbTag.Products.ToList())
        {
            product.Tags.Remove(dbTag);
        }

        _context.Tags.Remove(dbTag);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Tag removed successfully and related products updated." });
    }
    
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users.ToListAsync();

        return Ok(users);
    }
    
    [HttpGet("user-orders/{userId}")]
    public async Task<IActionResult> GetUserOrders(int userId)
    {
        var dbOrders = await _context.Orders.Where(o => o.UserId == userId).ToListAsync();

        return Ok(dbOrders);
    }
    
    [HttpDelete("remove-user/{id}")]
    public async Task<IActionResult> RemoveUser(int id)
    {
        var dbUser = await _context.Users.FindAsync(id);
        if (dbUser == null) 
            return NotFound(new { Message = "User not found" });
        
        var orders = await _context.Orders.Where(o => o.UserId == id).ToListAsync();
        foreach (var order in orders)
        {
            order.UserId = 0;
            _context.Orders.Update(order);
        }

        _context.Users.Remove(dbUser);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "User removed successfully" });
    }
    
    [HttpPost("add-user")]
    public async Task<IActionResult> AddUser([FromBody] User user)
    {
        var newUserId = 1;
        if (await _context.Users.AnyAsync())
            newUserId = await _context.Users.MaxAsync(u => u.UserId) + 1;

        user.UserId = newUserId;
        user.PasswordHash = _hasherService.HashPassword(user.PasswordHash);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "User added successfully"});
    }
    
    [HttpPost("add-order")]
    public async Task<IActionResult> AddOrder([FromBody] Order order)
    {
        var newOrderId = 1;
        if (await _context.Orders.AnyAsync())
            newOrderId = await _context.Orders.MaxAsync(o => o.OrderId) + 1;

        order.OrderId = newOrderId;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Order added successfully"});
    }
    
    [HttpGet("order-details")]
    public async Task<IActionResult> GetOrderDetails()
    {
        var orderDetails = await _context.OrderDetails.Include(od => od.Product).Include(od => od.Order).ToListAsync();
        return Ok(orderDetails);
    }
    
    [HttpPut("update-order/{id}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order updatedOrder)
    {
        var dbOrder = await _context.Orders.FindAsync(id);
        if (dbOrder == null)
            return NotFound(new { Message = "Order not found" });

        dbOrder.Status = updatedOrder.Status;
        dbOrder.OrderDate = updatedOrder.OrderDate;

        _context.Orders.Update(dbOrder);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Order updated successfully"});
    }
    
    [HttpDelete("remove-order-detail/{id}")]
    public async Task<IActionResult> RemoveOrderDetail(int id)
    {
        var dbOrderDetail = await _context.OrderDetails.FindAsync(id);
        if (dbOrderDetail == null)
            return NotFound(new { Message = "OrderDetail not found" });

        _context.OrderDetails.Remove(dbOrderDetail);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "OrderDetail removed successfully" });
    }
}