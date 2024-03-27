# SmartContract requirements

The backend expects certain data structures in the contract data and messages. Changing these structures (in future versions of contracts) may make the data unreadable and cause run-time errors.

A list of these expectations follows.

1. Master contract:
    * Internal [data layout](https://github.com/the-real-some-dao/alfa-mater-core/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/contracts/master.fc#L47) should be kept intact, because `DataParser.ParseMasterData()` parses data to obtain next indexes of all three child contracts, list of categories and languages;
    * [`get_order_address`](https://github.com/the-real-some-dao/alfa-mater-core/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/contracts/master.fc#L353), [`get_user_address`](https://github.com/the-real-some-dao/alfa-mater-core/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/contracts/master.fc#L358) and [`get_admin_address`](https://github.com/the-real-some-dao/alfa-mater-core/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/contracts/master.fc#L363) should return contract addresses by index;
2. Admin contract:
    * [`get_admin_data`](https://github.com/the-real-some-dao/alfa-mater-core/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/contracts/admin.fc#L201) should return values (expected by `DataParser.UpdateAdmin()`);
    * Content cell should keep structure of [buildAdminContent() in tests/utils/buildContent.ts](https://github.com/the-real-some-dao/alfa-mater-core/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/tests/utils/buildContent.ts#L44)
3. User contract:
    * [`get_user_data`](https://github.com/the-real-some-dao/alfa-mater-core/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/contracts/user.fc#L150) should return values (expected by `DataParser.UpdateUser()`);
    * Content cell should keep structure of [buildUserContent() in tests/utils/buildContent.ts](https://github.com/the-real-some-dao/alfa-mater-core/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/tests/utils/buildContent.ts#L59)
3. Order contract:
    * Internal [data layout](https://github.com/the-real-some-dao/alfa-mater-core/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/contracts/order.fc) should be kept intact, because `DataParser.ParseOrderData()` parses data to obtain all order values;
    * Content cell should keep structure of [buildOrderContent() in tests/utils/buildContent.ts](https://github.com/the-real-some-dao/alfa-mater-core/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/tests/utils/buildContent.ts#L74)
    * Status codes from [contracts/constants/constants.fc](https://github.com/the-real-some-dao/a-careers-smc/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/contracts/constants/constants.fc#L13-L24) should match values in `Order.cs`;
    * Op-codes from [contracts/constants/op-codes.fc](https://github.com/the-real-some-dao/alfa-mater-core/blob/eb63b74bb62b5cc4d9535e4576a0784b9b100f7e/contracts/constants/op-codes.fc#L3-L18) should match values in `OrderActivity.cs`;
    * Logic of [get_force_payment_availability](https://github.com/the-real-some-dao/a-careers-smc/blob/341669145a55dd99120d208c97e9da9f17a09f47/contracts/order.fc#L452-L465) and [get_refund_availability](https://github.com/the-real-some-dao/a-careers-smc/blob/341669145a55dd99120d208c97e9da9f17a09f47/contracts/order.fc#L467-L480) should match implementations of `Order.ForcePaymentAvailable` and `Order.RefundAvailable`;
    * Every update (from customer or freelances) should fire a message to Master contract (as a request for Order update).