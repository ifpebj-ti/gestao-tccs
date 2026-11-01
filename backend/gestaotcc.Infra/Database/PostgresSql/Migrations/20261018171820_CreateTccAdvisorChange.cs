using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class CreateTccAdvisorChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TccAdvisorChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TccId = table.Column<long>(type: "bigint", nullable: false),
                    RequesterUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequesterRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentAdvisorId = table.Column<long>(type: "bigint", nullable: false),
                    SuggestedAdvisorId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "PENDING_SIGNATURES"),
                    DocumentId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TccAdvisorChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TccAdvisorChanges_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TccAdvisorChanges_Tccs_TccId",
                        column: x => x.TccId,
                        principalTable: "Tccs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TccAdvisorChanges_Users_CurrentAdvisorId",
                        column: x => x.CurrentAdvisorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TccAdvisorChanges_Users_RequesterUserId",
                        column: x => x.RequesterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TccAdvisorChanges_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TccAdvisorChanges_Users_SuggestedAdvisorId",
                        column: x => x.SuggestedAdvisorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TccAdvisorChanges_CurrentAdvisorId",
                table: "TccAdvisorChanges",
                column: "CurrentAdvisorId");

            migrationBuilder.CreateIndex(
                name: "IX_TccAdvisorChanges_DocumentId",
                table: "TccAdvisorChanges",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_TccAdvisorChanges_RequesterUserId",
                table: "TccAdvisorChanges",
                column: "RequesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TccAdvisorChanges_ReviewedByUserId",
                table: "TccAdvisorChanges",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TccAdvisorChanges_SuggestedAdvisorId",
                table: "TccAdvisorChanges",
                column: "SuggestedAdvisorId");

            migrationBuilder.CreateIndex(
                name: "IX_TccAdvisorChanges_TccId",
                table: "TccAdvisorChanges",
                column: "TccId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TccAdvisorChanges");
        }
    }
}
