using gestaotcc.Application.Factories;
using gestaotcc.Domain.Entities.TccInvite;

namespace gestaotcc.Test.Factories;

public class TccInviteFactoryTests
{
    [Fact]
    public void CreateTccInvite_ShouldReturnTccInviteWithCorrectFields()
    {
        // Arrange
        var email   = "estudante@ifpe.edu.br";
        var code    = "ABC123";
        long campiId  = 10;
        long courseId = 20;

        // Act
        var invite = TccInviteFactory.CreateTccInvite(email, code, campiId, courseId);

        // Assert
        Assert.IsType<TccInviteEntity>(invite);
        Assert.Equal(email,    invite.Email);
        Assert.Equal(code,     invite.Code);
        Assert.Equal(campiId,  invite.CampiId);
        Assert.Equal(courseId, invite.CourseId);
        // Código recém-criado deve ser válido
        Assert.True(invite.IsValidCode);
        // Data de expiração deve ser futura
        Assert.True(invite.ExpirationDate > DateTime.UtcNow);
    }

    [Fact]
    public void CreateTccInvite_ShouldNotBeLinkedToAnyTcc_ByDefault()
    {
        var invite = TccInviteFactory.CreateTccInvite("x@ifpe.edu.br", "XYZ", 1, 1);

        Assert.Null(invite.Tcc);
        Assert.Null(invite.TccId);
    }
}

