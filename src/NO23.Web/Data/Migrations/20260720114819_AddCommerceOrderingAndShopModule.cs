using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NO23.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommerceOrderingAndShopModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "KitchenMenuItems",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MemberProfileId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    KitchenSubscriptionId = table.Column<int>(type: "integer", nullable: true),
                    DeliveryFullName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DeliveryPhoneNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeliveryAddressLine = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeliveryDistrict = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeliveryCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeliveryPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DeliveryTimeSlot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DeliveryFee = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_KitchenSubscriptions_KitchenSubscriptionId",
                        column: x => x.KitchenSubscriptionId,
                        principalTable: "KitchenSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Orders_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingCarts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MemberProfileId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingCarts_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Sku = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "character varying(700)", maxLength: 700, nullable: true),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShoppingCartId = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    KitchenMenuItemId = table.Column<int>(type: "integer", nullable: true),
                    ShopProductId = table.Column<int>(type: "integer", nullable: true),
                    ProductName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_KitchenMenuItems_KitchenMenuItemId",
                        column: x => x.KitchenMenuItemId,
                        principalTable: "KitchenMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CartItems_ShopProducts_ShopProductId",
                        column: x => x.ShopProductId,
                        principalTable: "ShopProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CartItems_ShoppingCarts_ShoppingCartId",
                        column: x => x.ShoppingCartId,
                        principalTable: "ShoppingCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    KitchenMenuItemId = table.Column<int>(type: "integer", nullable: true),
                    ShopProductId = table.Column<int>(type: "integer", nullable: true),
                    ProductName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_KitchenMenuItems_KitchenMenuItemId",
                        column: x => x.KitchenMenuItemId,
                        principalTable: "KitchenMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_ShopProducts_ShopProductId",
                        column: x => x.ShopProductId,
                        principalTable: "ShopProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_KitchenMenuItemId",
                table: "CartItems",
                column: "KitchenMenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ShoppingCartId",
                table: "CartItems",
                column: "ShoppingCartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ShopProductId",
                table: "CartItems",
                column: "ShopProductId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_KitchenMenuItemId",
                table: "OrderItems",
                column: "KitchenMenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ShopProductId",
                table: "OrderItems",
                column: "ShopProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_KitchenSubscriptionId",
                table: "Orders",
                column: "KitchenSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_MemberProfileId",
                table: "Orders",
                column: "MemberProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_MemberProfileId",
                table: "ShoppingCarts",
                column: "MemberProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopProducts_Sku",
                table: "ShopProducts",
                column: "Sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "ShoppingCarts");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "KitchenMenuItems");
        }
    }
}
