using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class AddTccBankingMemberGradeAndComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvaluationComments",
                table: "TccBankingMembers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Grade",
                table: "TccBankingMembers",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvaluationComments",
                table: "TccBankingMembers");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "TccBankingMembers");
        }
    }
}
