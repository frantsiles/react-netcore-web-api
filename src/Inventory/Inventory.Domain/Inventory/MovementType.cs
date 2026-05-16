namespace Inventory.Domain.Inventory;

public enum MovementType
{
    Receipt,         // stock entra (compra, producción)
    Adjustment,      // corrección manual (positiva o negativa)
    SalesReservation,// reserva para un SalesOrder confirmado
    SalesRelease,    // liberación de reserva (orden cancelada)
    WriteOff,        // baja (dañado, vencido, robado)
    TransferOut,     // salida por transferencia a otro almacén
    TransferIn       // entrada por transferencia desde otro almacén
}
