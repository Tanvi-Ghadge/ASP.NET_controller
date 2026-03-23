CREATE PROCEDURE sp_CreateEmployee
    @Name NVARCHAR(100),
    @Email NVARCHAR(255),
    @PasswordHash NVARCHAR(MAX),
    @Role NVARCHAR(50),
    @Salary DECIMAL(18,2),
    @HmacSecret NVARCHAR(MAX),
    @DepartmentId INT,
    @ManagerId INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Employees 
        (Name, Email, PasswordHash, [role], Salary, HmacSecret, DepartmentId, ManagerId)
    OUTPUT INSERTED.Id
    VALUES 
        (@Name, @Email, @PasswordHash, @Role, @Salary, @HmacSecret, @DepartmentId, @ManagerId);
END