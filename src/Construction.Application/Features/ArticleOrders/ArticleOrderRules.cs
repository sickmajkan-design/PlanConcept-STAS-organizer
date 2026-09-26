using Construction.Domain.Enums;

namespace Construction.Application.Features.ArticleOrders;

/// <summary>Who may ask for articles, who runs the orders, and which step may follow which.</summary>
public static class ArticleOrderRules
{
    public const int MaxItems = 30;

    /// <summary>The office side: orders, sends and declines. Whoever asks is not part of it.</summary>
    public static bool CanManage(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin or UserRole.ProjectManager;

    /// <summary>Which step may follow which. Who may take it is decided by the caller.</summary>
    public static bool CanMove(ArticleOrderStatus from, ArticleOrderStatus to) => (from, to) switch
    {
        (ArticleOrderStatus.Requested, ArticleOrderStatus.Ordered) => true,
        (ArticleOrderStatus.Ordered, ArticleOrderStatus.InDelivery) => true,
        (ArticleOrderStatus.InDelivery, ArticleOrderStatus.Delivered) => true,
        (ArticleOrderStatus.Requested or ArticleOrderStatus.Ordered, ArticleOrderStatus.Rejected) => true,
        (ArticleOrderStatus.Requested, ArticleOrderStatus.Cancelled) => true,
        _ => false
    };

    public static bool IsOpen(ArticleOrderStatus status) =>
        status is ArticleOrderStatus.Requested or ArticleOrderStatus.Ordered or ArticleOrderStatus.InDelivery;
}
