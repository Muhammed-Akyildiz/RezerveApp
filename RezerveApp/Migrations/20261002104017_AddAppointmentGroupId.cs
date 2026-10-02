using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RezerveApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupId",
                table: "Appointments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Appointments");
        }
    }
}
