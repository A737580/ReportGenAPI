CREATE OR REPLACE FUNCTION get_median_store_value_for_file(file_name_param TEXT)
RETURNS TABLE (value NUMERIC) AS $$ 
DECLARE
    median_val NUMERIC;
BEGIN
    SELECT PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY "StoreValue")
    INTO median_val
    FROM "Values"
    WHERE "FileName" = file_name_param;

    RETURN QUERY SELECT median_val;
END;
$$ LANGUAGE plpgsql;