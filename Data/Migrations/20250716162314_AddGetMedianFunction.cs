using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportGen.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGetMedianFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION get_median_store_value_for_file(file_name_param TEXT)
            RETURNS NUMERIC AS $$
            DECLARE
                median_val NUMERIC;
            BEGIN
                SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY ""StoreValue"")
                INTO median_val
                FROM ""Values""
                WHERE ""FileName"" = file_name_param;

                RETURN median_val;
            END;
            $$ LANGUAGE plpgsql;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS get_median_store_value_for_file(TEXT)");
        }
    }
}
