using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FileConnection.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Datas",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<byte[]>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datas", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    LastWriteTime = table.Column<DateTime>(nullable: false),
                    ParentID = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Nodes_Nodes_ParentID",
                        column: x => x.ParentID,
                        principalTable: "Nodes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leafs",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    LastWriteTime = table.Column<DateTime>(nullable: false),
                    ParentID = table.Column<long>(nullable: false),
                    DataID = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leafs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Leafs_Datas_DataID",
                        column: x => x.DataID,
                        principalTable: "Datas",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Leafs_Nodes_ParentID",
                        column: x => x.ParentID,
                        principalTable: "Nodes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeParameters",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false),
                    NodeID = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeParameters", x => x.ID);
                    table.ForeignKey(
                        name: "FK_NodeParameters_Nodes_NodeID",
                        column: x => x.NodeID,
                        principalTable: "Nodes",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeafParameters",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false),
                    LeafID = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeafParameters", x => x.ID);
                    table.ForeignKey(
                        name: "FK_LeafParameters_Leafs_LeafID",
                        column: x => x.LeafID,
                        principalTable: "Leafs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeafParameters_LeafID",
                table: "LeafParameters",
                column: "LeafID");

            migrationBuilder.CreateIndex(
                name: "IX_Leafs_DataID",
                table: "Leafs",
                column: "DataID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leafs_ParentID",
                table: "Leafs",
                column: "ParentID");

            migrationBuilder.CreateIndex(
                name: "IX_NodeParameters_NodeID",
                table: "NodeParameters",
                column: "NodeID");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_ParentID",
                table: "Nodes",
                column: "ParentID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeafParameters");

            migrationBuilder.DropTable(
                name: "NodeParameters");

            migrationBuilder.DropTable(
                name: "Leafs");

            migrationBuilder.DropTable(
                name: "Datas");

            migrationBuilder.DropTable(
                name: "Nodes");
        }
    }
}
