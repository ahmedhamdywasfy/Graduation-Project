using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHorse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Person2_HorseCore_And_Images : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Breeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Breeds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Colors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HorseStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorseStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Horses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BreedId = table.Column<int>(type: "int", nullable: false),
                    ColorId = table.Column<int>(type: "int", nullable: false),
                    GenderId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(7,2)", nullable: false),
                    Height = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MicrochipNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrentOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FatherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MotherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Horses_Breeds_BreedId",
                        column: x => x.BreedId,
                        principalTable: "Breeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Horses_Colors_ColorId",
                        column: x => x.ColorId,
                        principalTable: "Colors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Horses_Genders_GenderId",
                        column: x => x.GenderId,
                        principalTable: "Genders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Horses_HorseStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "HorseStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Horses_Horses_FatherId",
                        column: x => x.FatherId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Horses_Horses_MotherId",
                        column: x => x.MotherId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Horses_Users_CurrentOwnerId",
                        column: x => x.CurrentOwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HorseImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StorageId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorseImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorseImages_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OwnershipHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HorseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NewOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SaleDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnershipHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnershipHistories_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OwnershipHistories_Users_NewOwnerId",
                        column: x => x.NewOwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnershipHistories_Users_PreviousOwnerId",
                        column: x => x.PreviousOwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Breeds_Name",
                table: "Breeds",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colors_Name",
                table: "Colors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Genders_Name",
                table: "Genders",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HorseStatuses_Name",
                table: "HorseStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Horses_MicrochipNumber",
                table: "Horses",
                column: "MicrochipNumber",
                unique: true,
                filter: "[MicrochipNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_RegistrationNumber",
                table: "Horses",
                column: "RegistrationNumber",
                unique: true,
                filter: "[RegistrationNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_Name",
                table: "Horses",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_CreatedAt",
                table: "Horses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_StatusId",
                table: "Horses",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_CurrentOwnerId",
                table: "Horses",
                column: "CurrentOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_BreedId",
                table: "Horses",
                column: "BreedId");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_ColorId",
                table: "Horses",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_GenderId",
                table: "Horses",
                column: "GenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_FatherId",
                table: "Horses",
                column: "FatherId");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_MotherId",
                table: "Horses",
                column: "MotherId");

            migrationBuilder.CreateIndex(
                name: "IX_HorseImages_HorseId",
                table: "HorseImages",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_HorseImages_HorseId_ContentHash",
                table: "HorseImages",
                columns: new[] { "HorseId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnershipHistories_HorseId_ChangedAtUtc",
                table: "OwnershipHistories",
                columns: new[] { "HorseId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnershipHistories_HorseId_SaleDate",
                table: "OwnershipHistories",
                columns: new[] { "HorseId", "SaleDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnershipHistories_NewOwnerId",
                table: "OwnershipHistories",
                column: "NewOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnershipHistories_PreviousOwnerId",
                table: "OwnershipHistories",
                column: "PreviousOwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorseImages");

            migrationBuilder.DropTable(
                name: "OwnershipHistories");

            migrationBuilder.DropTable(
                name: "Horses");

            migrationBuilder.DropTable(
                name: "Breeds");

            migrationBuilder.DropTable(
                name: "Colors");

            migrationBuilder.DropTable(
                name: "Genders");

            migrationBuilder.DropTable(
                name: "HorseStatuses");
        }
    }
}
