# SmartContract requirements

The backend expects certain data structures in the contract data and messages. Changing these structures (in future versions of contracts) may make the data unreadable and cause run-time errors.

A list of these expectations follows.

1. Master contract:
    * Internal [data layout](https://github.com/the-real-some-dao/alfa-mater-core/blob/8856b8a49d75d4cda8d491e1ca14c37f55007be5/contracts/master.fc#L47) should be kept intact, because [`SyncTask.SyncMaster()`](/backend/Services/SyncTask.cs) parses data to obtain next indexes of all three child contracts, list of categories and languages;
    * [`get_order_address`](https://github.com/the-real-some-dao/alfa-mater-core/blob/8856b8a49d75d4cda8d491e1ca14c37f55007be5/contracts/master.fc#L353), [`get_user_address`](https://github.com/the-real-some-dao/alfa-mater-core/blob/8856b8a49d75d4cda8d491e1ca14c37f55007be5/contracts/master.fc#L358) and [`get_admin_address`](https://github.com/the-real-some-dao/alfa-mater-core/blob/8856b8a49d75d4cda8d491e1ca14c37f55007be5/contracts/master.fc#L363) should return contract addresses by index;
2. Admin contract:
    * [`get_admin_data`](https://github.com/the-real-some-dao/alfa-mater-core/blob/8856b8a49d75d4cda8d491e1ca14c37f55007be5/contracts/admin.fc#L201) return value is expected by [`DataParser.UpdateAdmin()`](backend/Services/DataParser.cs);
    * Contact cell should keep structure of [buildAdminContent() in tests/utils/buildContent.ts](https://github.com/the-real-some-dao/alfa-mater-core/blob/8856b8a49d75d4cda8d491e1ca14c37f55007be5/tests/utils/buildContent.ts#L44)
3. User contract:
    * [`get_user_data`](https://github.com/the-real-some-dao/alfa-mater-core/blob/8856b8a49d75d4cda8d491e1ca14c37f55007be5/contracts/user.fc#L150) return value is expected by [`DataParser.UpdateUser()`](backend/Services/DataParser.cs);
    * Contact cell should keep structure of [buildUserContent() in tests/utils/buildContent.ts](https://github.com/the-real-some-dao/alfa-mater-core/blob/8856b8a49d75d4cda8d491e1ca14c37f55007be5/tests/utils/buildContent.ts#L59)
3. Order contract:
    * [`get_order_data`](https://github.com/the-real-some-dao/alfa-mater-core/blob/8856b8a49d75d4cda8d491e1ca14c37f55007be5/contracts/order.fc#L423) return value is expected by [`DataParser.UpdateOrder()`](backend/Services/DataParser.cs);
    * Contact cell should keep structure of [buildOrderContent() in tests/utils/buildContent.ts](https://github.com/the-real-some-dao/alfa-mater-core/blob/8856b8a49d75d4cda8d491e1ca14c37f55007be5/tests/utils/buildContent.ts#L74)    
    * Op-codes from [contracts/constants/op-codes.fc](https://github.com/the-real-some-dao/alfa-mater-core/blob/8856b8a49d75d4cda8d491e1ca14c37f55007be5/contracts/constants/op-codes.fc#L3-L18) should match list in [OpCode.cs](backend/data/OpCode.cs);
    * Every update (from customer or freelances) should fire a message to Master contract (as a request for Order update).