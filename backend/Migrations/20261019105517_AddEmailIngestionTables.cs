using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailIngestionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Snippet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsJobRelated = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MailboxConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GoogleEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastHistoryId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConnectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailboxConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingJobApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailMessageId = table.Column<int>(type: "int", nullable: false),
                    CompanyNameGuess = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobTitleGuess = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusGuess = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LinkedJobApplicationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingJobApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingJobApplications_EmailMessages_EmailMessageId",
                        column: x => x.EmailMessageId,
                        principalTable: "EmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingJobApplications_JobApplications_LinkedJobApplicationId",
                        column: x => x.LinkedJobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PendingStatusChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailMessageId = table.Column<int>(type: "int", nullable: false),
                    SuggestedJobApplicationId = table.Column<int>(type: "int", nullable: true),
                    StatusGuess = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoteGuess = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingStatusChanges_EmailMessages_EmailMessageId",
                        column: x => x.EmailMessageId,
                        principalTable: "EmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingStatusChanges_JobApplications_SuggestedJobApplicationId",
                        column: x => x.SuggestedJobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistory_SourceEmailId",
                table: "ApplicationStatusHistory",
                column: "SourceEmailId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_UserId_ExternalMessageId",
                table: "EmailMessages",
                columns: new[] { "UserId", "ExternalMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailboxConnections_UserId_Provider",
                table: "MailboxConnections",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingJobApplications_EmailMessageId",
                table: "PendingJobApplications",
                column: "EmailMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingJobApplications_LinkedJobApplicationId",
                table: "PendingJobApplications",
                column: "LinkedJobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingStatusChanges_EmailMessageId",
                table: "PendingStatusChanges",
                column: "EmailMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingStatusChanges_SuggestedJobApplicationId",
                table: "PendingStatusChanges",
                column: "SuggestedJobApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationStatusHistory_EmailMessages_SourceEmailId",
                table: "ApplicationStatusHistory",
                column: "SourceEmailId",
                principalTable: "EmailMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationStatusHistory_EmailMessages_SourceEmailId",
                table: "ApplicationStatusHistory");

            migrationBuilder.DropTable(
                name: "MailboxConnections");

            migrationBuilder.DropTable(
                name: "PendingJobApplications");

            migrationBuilder.DropTable(
                name: "PendingStatusChanges");

            migrationBuilder.DropTable(
                name: "EmailMessages");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationStatusHistory_SourceEmailId",
                table: "ApplicationStatusHistory");
        }
    }
}
