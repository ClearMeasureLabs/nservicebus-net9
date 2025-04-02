if object_id('[orders].[Order]', 'U') is null
    create table [orders].[Order] (
        Id uniqueidentifier not null primary key,
        Status nvarchar(50) not null,
        OrderNumber nvarchar(50) not null,
        ProductCode nvarchar(50) not null,
        Quantity int not null,
        VendorName nvarchar(50) not null
    )

-- Create unique index on OrderNumber
if not exists (select * from sys.indexes where name = 'IX_Order_OrderNumber')
    create unique index IX_Order_OrderNumber on [orders].[Order] (OrderNumber)