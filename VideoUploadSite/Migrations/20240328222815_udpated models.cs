using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoUploadSite.Migrations
{
    /// <inheritdoc />
    public partial class udpatedmodels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProcessedVideoUrl",
                table: "Videos",
                newName: "VideoBlobUrl");

            migrationBuilder.RenameColumn(
                name: "OriginalVideoUrl",
                table: "Videos",
                newName: "ProcessedVideoBlobUrl");

            migrationBuilder.AddColumn<bool>(
                name: "shouldGenerateThumbnail",
                table: "Videos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shouldGenerateThumbnail",
                table: "Videos");

            migrationBuilder.RenameColumn(
                name: "VideoBlobUrl",
                table: "Videos",
                newName: "ProcessedVideoUrl");

            migrationBuilder.RenameColumn(
                name: "ProcessedVideoBlobUrl",
                table: "Videos",
                newName: "OriginalVideoUrl");
        }
    }
}
