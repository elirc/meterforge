using MeterForge.Domain.Common;
using MeterForge.Domain.Payments;

namespace MeterForge.Domain.Tests.Payments;

public sealed class PaymentAttemptTests
{
    [Fact]
    public void Retry_FailedAttempt_ReturnsToPendingAndIncrementsAttempt()
    {
        var payment = new PaymentAttempt(Guid.NewGuid(), "tenant-a", Guid.NewGuid(), new Money(10m, "USD"), "key-1");
        payment.Fail();
        payment.Retry();

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(2, payment.AttemptNumber);
    }

    [Fact]
    public void Succeed_RecordsProviderEvidence()
    {
        var payment = new PaymentAttempt(Guid.NewGuid(), "tenant-a", Guid.NewGuid(), new Money(10m, "USD"), "key-1");
        payment.Succeed("provider-1");
        Assert.Equal("provider-1", payment.ProviderReference);
    }
}
