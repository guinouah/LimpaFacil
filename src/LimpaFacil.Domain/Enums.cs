namespace LimpaFacil.Domain;

public enum UserRole { Client = 0, Employee = 1, Administrator = 2 }

public enum CustomerType { Individual = 0, Company = 1 }

public enum OrderStatus
{
    Received = 0,
    InSeparation = 1,
    ReadyForPickup = 2,
    Completed = 3,
    Cancelled = 4
}

public static class OrderStatusExtensions
{
    public static string ToPtBr(this OrderStatus status) => status switch
    {
        OrderStatus.Received => "Recebido",
        OrderStatus.InSeparation => "Em separação",
        OrderStatus.ReadyForPickup => "Pronto para retirada",
        OrderStatus.Completed => "Concluído",
        OrderStatus.Cancelled => "Cancelado",
        _ => status.ToString()
    };
}
