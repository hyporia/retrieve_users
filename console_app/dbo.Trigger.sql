CREATE TRIGGER inst_upd
	ON [dbo].[Users]
	INSTEAD OF UPDATE
	AS
	BEGIN
		update 
			Users 
			set Name = a.Name,
				City = a.City,
				Phone = a.Phone,
				UpdDate = a.UpdDate
			from inserted a, deleted b
			where a.Id = Users.Id and 
					a.Id = b.Id
					and a.UpdDate > b.UpdDate
	END
