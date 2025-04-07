namespace axp.Dbc
{
    public struct DbcHead
    {
        /// <summary>
        /// Chữ ký 0xDDBBCC00
        /// </summary>
        public uint Identity;
        /// <summary>
        /// Số trường (Danh sách)
        /// </summary>
        public int FieldCount;
        /// <summary>
        /// Hàng dữ liệu
        /// </summary>
        public int RecordCount;
        /// <summary>
        /// Kích thước vùng chuỗi
        /// </summary>
        public int StringBlockSize;
    }
}