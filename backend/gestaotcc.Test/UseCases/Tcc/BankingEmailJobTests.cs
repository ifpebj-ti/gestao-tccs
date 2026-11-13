using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccBankingMember;
using gestaotcc.Domain.Entities.TccSchedule;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class BankingEmailJobTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly BankingEmailJob _job;

    public BankingEmailJobTests()
    {
        _job = new BankingEmailJob(_tccGateway, _emailGateway);
    }

    [Fact]
    public async Task SendReminderEmails_ShouldDoNothing_WhenTccOrScheduleIsNull()
    {
        _tccGateway.FindTccScheduling(1).Returns((TccEntity?)null);

        await _job.SendReminderEmails(1);

        await _emailGateway.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Fact]
    public async Task SendReminderEmails_ShouldSendEmailsToInternalAndExternalMembers()
    {
        var schedule = new TccScheduleEntity(1, DateTime.UtcNow.AddDays(1), "Sala 10", 1);
        var tcc = new TccEntity { Id = 1, Title = "TCC Inteligente", TccSchedule = schedule };

        var internalMember = new TccBankingMemberEntity("Examinador Int", "int@teste.com", "Examinador Interno", "", 1)
        {
            UserId = 10
        };
        var externalMember = new TccBankingMemberEntity("Examinador Ext", "ext@teste.com", "Examinador Externo", "token", 1);
        var emptyEmailMember = new TccBankingMemberEntity("Sem Email", "", "Examinador", "token2", 1);

        tcc.BankingMembers.Add(internalMember);
        tcc.BankingMembers.Add(externalMember);
        tcc.BankingMembers.Add(emptyEmailMember);

        _tccGateway.FindTccScheduling(1).Returns(tcc);

        await _job.SendReminderEmails(1);

        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(e => e.Recipient == "int@teste.com"));
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(e => e.Recipient == "ext@teste.com"));
    }
}

