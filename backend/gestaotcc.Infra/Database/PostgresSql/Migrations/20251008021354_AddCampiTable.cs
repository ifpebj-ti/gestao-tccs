using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace gestaotcc.Infra.Database.PostgresSql.Migrations
{
    /// <inheritdoc />
    public partial class AddCampiTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Courses_CourseId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CourseId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Courses",
                newName: "Level");

            migrationBuilder.AddColumn<long>(
                name: "CampiCourseId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Campi",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CampiCourses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampiId = table.Column<long>(type: "bigint", nullable: false),
                    CourseId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampiCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampiCourses_Campi_CampiId",
                        column: x => x.CampiId,
                        principalTable: "Campi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampiCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CampiCourseId",
                table: "Users",
                column: "CampiCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CampiCourses_CampiId",
                table: "CampiCourses",
                column: "CampiId");

            migrationBuilder.CreateIndex(
                name: "IX_CampiCourses_CourseId",
                table: "CampiCourses",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_CampiCourses_CampiCourseId",
                table: "Users",
                column: "CampiCourseId",
                principalTable: "CampiCourses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_CampiCourses_CampiCourseId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "CampiCourses");

            migrationBuilder.DropTable(
                name: "Campi");

            migrationBuilder.DropIndex(
                name: "IX_Users_CampiCourseId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CampiCourseId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "Courses",
                newName: "Description");

            migrationBuilder.AddColumn<long>(
                name: "CourseId",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CourseId",
                table: "Users",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Courses_CourseId",
                table: "Users",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
