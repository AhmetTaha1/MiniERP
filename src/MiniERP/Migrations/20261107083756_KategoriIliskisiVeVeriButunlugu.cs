using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniERP.Migrations
{
    /// <inheritdoc />
    public partial class KategoriIliskisiVeVeriButunlugu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faturalar_Siparisler_SiparisId",
                table: "Faturalar");

            migrationBuilder.DropForeignKey(
                name: "FK_SiparisDetaylar_Urunler_UrunId",
                table: "SiparisDetaylar");

            migrationBuilder.DropForeignKey(
                name: "FK_Siparisler_Musteriler_MusteriId",
                table: "Siparisler");


            migrationBuilder.AlterColumn<decimal>(
                name: "Fiyat",
                table: "Urunler",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            // --- Product category: free text -> foreign key (data-preserving) ---
            migrationBuilder.AddColumn<int>(
                name: "KategoriId",
                table: "Urunler",
                type: "integer",
                nullable: true);

            // 1) Every category text used by a product must exist in Kategoriler (case-insensitive).
            migrationBuilder.Sql(@"
                INSERT INTO ""Kategoriler"" (""Ad"", ""OlusturmaTarihi"")
                SELECT DISTINCT ON (lower(btrim(u.""Kategori""))) left(btrim(u.""Kategori""), 100), now()
                FROM ""Urunler"" u
                WHERE btrim(u.""Kategori"") <> ''
                  AND NOT EXISTS (SELECT 1 FROM ""Kategoriler"" k WHERE lower(k.""Ad"") = lower(btrim(u.""Kategori"")));

                INSERT INTO ""Kategoriler"" (""Ad"", ""OlusturmaTarihi"")
                SELECT 'Uncategorized', now()
                WHERE EXISTS (SELECT 1 FROM ""Urunler"" WHERE btrim(""Kategori"") = '')
                  AND NOT EXISTS (SELECT 1 FROM ""Kategoriler"" WHERE lower(""Ad"") = 'uncategorized');");

            // 2) Link products to their category rows.
            migrationBuilder.Sql(@"
                UPDATE ""Urunler"" u
                SET ""KategoriId"" = k.""Id""
                FROM ""Kategoriler"" k
                WHERE lower(k.""Ad"") = lower(CASE WHEN btrim(u.""Kategori"") = '' THEN 'Uncategorized' ELSE left(btrim(u.""Kategori""), 100) END);");

            migrationBuilder.AlterColumn<int>(
                name: "KategoriId",
                table: "Urunler",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Kategori",
                table: "Urunler");

            // --- Order status: store enum names (ASCII) instead of Turkish display text ---
            migrationBuilder.Sql(@"
                UPDATE ""Siparisler"" SET ""Durum"" = CASE ""Durum""
                    WHEN 'Hazırlanıyor' THEN 'Hazirlaniyor'
                    WHEN 'Tamamlandı' THEN 'Tamamlandi'
                    WHEN 'İptal' THEN 'Iptal'
                    ELSE ""Durum"" END;");

            migrationBuilder.AlterColumn<decimal>(
                name: "ToplamTutar",
                table: "Siparisler",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Durum",
                table: "Siparisler",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "BirimFiyat",
                table: "SiparisDetaylar",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Soyad",
                table: "Musteriler",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.Sql(@"UPDATE ""Musteriler"" SET ""Soyad"" = NULL WHERE btrim(""Soyad"") = '';");

            // --- Audit log: normalize legacy Turkish module/action names to the current English values ---
            migrationBuilder.Sql(@"
                UPDATE ""IslemLoglari"" SET ""Modul"" = CASE ""Modul""
                    WHEN 'Kullanıcı' THEN 'User' WHEN 'Müşteri' THEN 'Customer' WHEN 'Ürün' THEN 'Product'
                    WHEN 'Kategori' THEN 'Category' WHEN 'Sipariş' THEN 'Order' WHEN 'Fatura' THEN 'Invoice'
                    ELSE ""Modul"" END,
                ""Islem"" = CASE ""Islem""
                    WHEN 'Oluşturma' THEN 'Created' WHEN 'Güncelleme' THEN 'Updated' WHEN 'Silme' THEN 'Deleted'
                    WHEN 'Pasife Alma' THEN 'Deactivated' WHEN 'Durum Güncelleme' THEN 'Status Updated'
                    WHEN 'Ödeme Durumu' THEN 'Payment Status'
                    ELSE ""Islem"" END;");

            migrationBuilder.AlterColumn<decimal>(
                name: "ToplamTutar",
                table: "Faturalar",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateIndex(
                name: "IX_Urunler_KategoriId",
                table: "Urunler",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_Siparisler_Durum",
                table: "Siparisler",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_Siparisler_SiparisTarihi",
                table: "Siparisler",
                column: "SiparisTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_Musteriler_Email",
                table: "Musteriler",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_Siparisler_SiparisId",
                table: "Faturalar",
                column: "SiparisId",
                principalTable: "Siparisler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiparisDetaylar_Urunler_UrunId",
                table: "SiparisDetaylar",
                column: "UrunId",
                principalTable: "Urunler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Siparisler_Musteriler_MusteriId",
                table: "Siparisler",
                column: "MusteriId",
                principalTable: "Musteriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Urunler_Kategoriler_KategoriId",
                table: "Urunler",
                column: "KategoriId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faturalar_Siparisler_SiparisId",
                table: "Faturalar");

            migrationBuilder.DropForeignKey(
                name: "FK_SiparisDetaylar_Urunler_UrunId",
                table: "SiparisDetaylar");

            migrationBuilder.DropForeignKey(
                name: "FK_Siparisler_Musteriler_MusteriId",
                table: "Siparisler");

            migrationBuilder.DropForeignKey(
                name: "FK_Urunler_Kategoriler_KategoriId",
                table: "Urunler");

            migrationBuilder.DropIndex(
                name: "IX_Urunler_KategoriId",
                table: "Urunler");

            migrationBuilder.DropIndex(
                name: "IX_Siparisler_Durum",
                table: "Siparisler");

            migrationBuilder.DropIndex(
                name: "IX_Siparisler_SiparisTarihi",
                table: "Siparisler");

            migrationBuilder.DropIndex(
                name: "IX_Musteriler_Email",
                table: "Musteriler");


            migrationBuilder.AlterColumn<decimal>(
                name: "Fiyat",
                table: "Urunler",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "Kategori",
                table: "Urunler",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE ""Urunler"" u SET ""Kategori"" = k.""Ad""
                FROM ""Kategoriler"" k WHERE k.""Id"" = u.""KategoriId"";

                UPDATE ""Siparisler"" SET ""Durum"" = CASE ""Durum""
                    WHEN 'Hazirlaniyor' THEN 'Hazırlanıyor'
                    WHEN 'Tamamlandi' THEN 'Tamamlandı'
                    WHEN 'Iptal' THEN 'İptal'
                    ELSE ""Durum"" END;

                UPDATE ""Musteriler"" SET ""Soyad"" = '' WHERE ""Soyad"" IS NULL;");

            migrationBuilder.DropColumn(
                name: "KategoriId",
                table: "Urunler");

            migrationBuilder.AlterColumn<decimal>(
                name: "ToplamTutar",
                table: "Siparisler",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Durum",
                table: "Siparisler",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "BirimFiyat",
                table: "SiparisDetaylar",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Soyad",
                table: "Musteriler",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ToplamTutar",
                table: "Faturalar",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddForeignKey(
                name: "FK_Faturalar_Siparisler_SiparisId",
                table: "Faturalar",
                column: "SiparisId",
                principalTable: "Siparisler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SiparisDetaylar_Urunler_UrunId",
                table: "SiparisDetaylar",
                column: "UrunId",
                principalTable: "Urunler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Siparisler_Musteriler_MusteriId",
                table: "Siparisler",
                column: "MusteriId",
                principalTable: "Musteriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
