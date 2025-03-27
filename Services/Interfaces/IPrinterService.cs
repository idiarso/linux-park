using System;
using System.Threading.Tasks;

namespace ParkIRC.Services.Interfaces
{
    public interface IPrinterService
    {
        Task<bool> PrintTicket(string ticketNumber, string entryTime);
        Task<bool> PrintReceipt(string ticketNumber, string exitTime, decimal amount);
        Task<bool> TestPrinter();
        Task<bool> PrintTicketWithBarcode(string ticketNumber, DateTime entryTime, string barcodeImagePath);
    }
} 