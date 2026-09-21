using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeAndEvaluationCommentsToBankingMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Grade",
                table: "TccBankingMembers",
                type: "numeric(5,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Grade",
                table: "TccBankingMembers",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldNullable: true);
        }
    }
}
