using System;
using System.Threading.Tasks;

namespace ParkIRC.Services
{
    public interface IPrinterService
    {
        Task InitializeAsync();
        Task<bool> PrintTicketAsync(string ticketNumber, string entryTime);
        Task<bool> PrintReceiptAsync(string ticketNumber, string exitTime, decimal amount);
        Task<bool> PrintEntryTicketAsync(string ticketNumber);
        Task<bool> CheckPrinterStatusAsync();
        ValueTask DisposeAsync();
        Task<bool> PrintTicket(string ticketNumber, string entryTime);
        Task<bool> PrintReceipt(string ticketNumber, string exitTime, decimal amount);
        Task<bool> PrintTicketWithBarcode(string ticketNumber, DateTime entryTime, string barcodeImagePath);
        Task<bool> PrintTicket(string ticketContent);
        Task<bool> TestPrinter();
    }
} 