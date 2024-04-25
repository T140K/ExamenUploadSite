using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoUploadSite.Migrations
{
    /// <inheritdoc />
    public partial class AddingProcessingColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "shouldGenerateThumbnail",
                table: "Videos",
                newName: "ShouldGenerateThumbnail");

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "Videos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "Videos");

            migrationBuilder.RenameColumn(
                name: "ShouldGenerateThumbnail",
                table: "Videos",
                newName: "shouldGenerateThumbnail");
        }
    }
}
