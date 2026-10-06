using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresalesGPT.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeckLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Layout",
                table: "DeckSlides",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LayoutData",
                table: "DeckSlides",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Layout",
                table: "DeckSlides");

            migrationBuilder.DropColumn(
                name: "LayoutData",
                table: "DeckSlides");
        }
    }
}
