CREATE TABLE [dbo].[SyncInfo](
	[TableName] [nchar](50) NOT NULL,
	[ModifyTime] [datetime2](7) NULL,
	[SyncTime] [datetime2](7) NULL
) ON [PRIMARY]


CREATE TABLE ZHIFY.SYNCINFO
  (
    TABLENAME VARCHAR2(50 BYTE) NOT NULL ENABLE,
    MODIFYTIME DATE,
    SYNCTIME DATE
  )