using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresalesGPT.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSowAndDeck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeckSlides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OpportunityId = table.Column<int>(type: "INTEGER", nullable: false),
                    SlideOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Bullets = table.Column<string>(type: "TEXT", nullable: false),
                    SpeakerNotes = table.Column<string>(type: "TEXT", nullable: false),
                    SourceSection = table.Column<string>(type: "TEXT", nullable: false),
                    GeneratedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeckSlides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SowSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OpportunityId = table.Column<int>(type: "INTEGER", nullable: false),
                    SectionOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    SectionName = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CoveredRequirements = table.Column<string>(type: "TEXT", nullable: false),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEdited = table.Column<bool>(type: "INTEGER", nullable: false),
                    GeneratedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SowSections", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeckSlides");

            migrationBuilder.DropTable(
                name: "SowSections");
        }
    }
}
