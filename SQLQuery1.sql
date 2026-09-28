SELECT 
    bs.Id,
    bs.BusinessId,
    bs.SubscriptionPlanId,
    sp.Name AS PlanName,
    sp.MaxEmployees,
    bs.BillingPeriod,
    bs.IsActive,
    bs.StartDate,
    bs.EndDate
FROM BusinessSubscriptions bs
INNER JOIN SubscriptionPlans sp 
    ON bs.SubscriptionPlanId = sp.Id
ORDER BY bs.BusinessId, bs.StartDate DESC;