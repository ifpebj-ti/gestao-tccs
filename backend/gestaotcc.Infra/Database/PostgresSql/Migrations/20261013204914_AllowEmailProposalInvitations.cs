using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class AllowEmailProposalInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "ProposalParticipants",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ProposalParticipants",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "NotificationDismissedAt",
                table: "ProposalParticipants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ProposalParticipants" AS invitation
                SET "Email" = lower(trim(account."Email"))
                FROM "Users" AS account WHERE account."Id" = invitation."UserId";
                UPDATE "Tccs" AS proposal SET "Status" = 'AWAITING_PARTICIPANTS'
                WHERE proposal."Status" = 'PENDING_APPROVAL' AND EXISTS
                  (SELECT 1 FROM "ProposalParticipants" AS invitation
                   WHERE invitation."TccId" = proposal."Id" AND invitation."Status" = 'PENDING');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ProposalParticipants_TccId_Email",
                table: "ProposalParticipants",
                columns: new[] { "TccId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The previous schema cannot represent an invitation without an account.
            // Refuse the downgrade rather than deleting invitations or inventing user IDs.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM "ProposalParticipants" WHERE "UserId" IS NULL) THEN
                    RAISE EXCEPTION 'Não é possível reverter enquanto houver convites por e-mail sem conta vinculada.';
                  END IF;
                END $$;
                UPDATE "Tccs" SET "Status" = 'PENDING_APPROVAL' WHERE "Status" = 'AWAITING_PARTICIPANTS';
                """);
            migrationBuilder.DropIndex(
                name: "IX_ProposalParticipants_TccId_Email",
                table: "ProposalParticipants");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "ProposalParticipants");

            migrationBuilder.DropColumn(
                name: "NotificationDismissedAt",
                table: "ProposalParticipants");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "ProposalParticipants",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
