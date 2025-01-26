namespace ProjektM.Requests;

public class CreateOrderRequest
{
    public int UserId { get; set; } 
    public List<OrderItemRequest> Items { get; set; } 
}