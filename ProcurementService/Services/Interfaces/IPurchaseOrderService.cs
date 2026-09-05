using ProcurementService.DTOs.PurchaseOrders;

namespace ProcurementService.Services.Interfaces;

public interface IPurchaseOrderService
{
    Task<List<PurchaseOrderResponse>> GetAllAsync();
    Task<PurchaseOrderResponse?> GetByGuidAsync(Guid guid);
    Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request);
    Task<PurchaseOrderResponse?> UpdateAsync(Guid guid, UpdatePurchaseOrderRequest request);
    Task<PurchaseOrderResponse?> CancelAsync(Guid guid);
    Task<bool> DeleteAsync(Guid guid);

    // Approval Workflow Transitions
    Task<PurchaseOrderResponse?> SubmitForApprovalAsync(Guid guid);
    Task<PurchaseOrderResponse?> ApproveAsync(Guid guid);
    Task<PurchaseOrderResponse?> RejectAsync(Guid guid, RejectPurchaseOrderRequest? request);
    Task<PurchaseOrderResponse?> MarkAsSentAsync(Guid guid);
    Task<PurchaseOrderResponse?> MarkAsReceivedAsync(Guid guid);
}
