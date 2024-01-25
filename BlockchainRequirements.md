# Blockchain requirements

The backend expects certain data structures in the contract data and messages. Changing these structures (in future versions of contracts) may make the data unreadable and cause run-time errors.

A list of these expectations follows.

1. Content cells:
    * Admin contract: [AdminContent.FillFrom()](backend/Data/AdminContent.cs) must match [buildAdminContent() in tests/utils/buildContent.ts#7fb025b](https://github.com/the-real-some-dao/alfa-mater-core/blob/7fb025b6bfc24e4bb7219f6ea65f4184dccaa29e/tests/utils/buildContent.ts#L44);
    * User contract: [UserContent.FillFrom()](backend/Data/UserContent.cs) must match [buildUserContent() in tests/utils/buildContent.ts#7fb025b](https://github.com/the-real-some-dao/alfa-mater-core/blob/7fb025b6bfc24e4bb7219f6ea65f4184dccaa29e/tests/utils/buildContent.ts#L59);
    * Order contract: [OrderContent.FillFrom()](backend/Data/OrderContent.cs) must match [buildOrderContent() in tests/utils/buildContent.ts#7fb025b](https://github.com/the-real-some-dao/alfa-mater-core/blob/7fb025b6bfc24e4bb7219f6ea65f4184dccaa29e/tests/utils/buildContent.ts#L74);
2. Get methods:
    * Admin contract: [DataParser.GetAdmin()](backend/Services/DataParser.cs) must match [get_admin_data() in contracts/admin.fc#7fb025b](https://github.com/the-real-some-dao/alfa-mater-core/blob/7fb025b6bfc24e4bb7219f6ea65f4184dccaa29e/contracts/admin.fc#L199);
    * User contract: [DataParser.GetUser()](backend/Services/DataParser.cs) must match [get_user_data() in contracts/user.fc#7fb025b](https://github.com/the-real-some-dao/alfa-mater-core/blob/7fb025b6bfc24e4bb7219f6ea65f4184dccaa29e/contracts/user.fc#L148);
    * Order contract: [DataParser.GetOrder()](backend/Services/DataParser.cs) must match [get_order_data() in contracts/order.fc#7fb025b](https://github.com/the-real-some-dao/alfa-mater-core/blob/7fb025b6bfc24e4bb7219f6ea65f4184dccaa29e/contracts/order.fc#L392);