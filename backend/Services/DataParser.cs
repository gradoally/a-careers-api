using SomeDAO.Backend.Data;
using TonLibDotNet;
using TonLibDotNet.Types.Smc;

namespace SomeDAO.Backend.Services
{
	public class DataParser(ITonClient tonClient)
	{
		public async Task<Admin> GetAdmin(string address)
		{
			await tonClient.InitIfNeeded().ConfigureAwait(false);

			var smc = await tonClient.SmcLoad(address).ConfigureAwait(false);
			var data = await tonClient.SmcRunGetMethod(smc.Id, new MethodIdName("get_admin_data")).ConfigureAwait(false);
			await tonClient.SmcForget(smc.Id).ConfigureAwait(false);

			if (data.ExitCode != 0)
			{
				throw new NonZeroExitCodeException(data.ExitCode, address, "get_admin_data");
			}

			// Method returns:
			// (int, int, slice, slice, int, cell) get_admin_data()
			// (storage::init?, storage::index, storage::master_address, storage::admin_address, storage::revoked_at, storage::content)

			var index = data.Stack[1].ToLong();
			var adminAddress = data.Stack[3].ToBoc().RootCells[0].BeginRead().LoadAddressIntStd(false);
			var revokedAt = data.Stack[4].ToInt();
			var content = data.Stack[5].ToBoc();

			var admin = new Admin()
			{
				Index = index,
				Address = TonUtils.Address.SetBounceable(address, true),
				AdminAddress = adminAddress,
				RevokedAt = revokedAt == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(revokedAt),
			};

			admin.FillFrom(content.RootCells[0]);

			return admin;
		}

		public async Task<User> GetUser(string address)
		{
			await tonClient.InitIfNeeded().ConfigureAwait(false);

			var smc = await tonClient.SmcLoad(address).ConfigureAwait(false);
			var data = await tonClient.SmcRunGetMethod(smc.Id, new MethodIdName("get_user_data")).ConfigureAwait(false);
			await tonClient.SmcForget(smc.Id).ConfigureAwait(false);

			if (data.ExitCode != 0)
			{
				throw new NonZeroExitCodeException(data.ExitCode, address, "get_user_data");
			}

			// Method returns:
			// (int, int, slice, slice, int, cell) get_user_data()
			// (storage::init?, storage::index, storage::master_address, storage::user_address, storage::revoked_at, storage::content)

			var index = data.Stack[1].ToLong();
			var userAddress = data.Stack[3].ToBoc().RootCells[0].BeginRead().LoadAddressIntStd(false);
			var revokedAt = data.Stack[4].ToInt();
			var content = data.Stack[5].ToBoc();

			var user = new User()
			{
				Index = index,
				Address = TonUtils.Address.SetBounceable(address, true),
				UserAddress = userAddress,
				RevokedAt = revokedAt == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(revokedAt),
			};

			user.FillFrom(content.RootCells[0]);

			return user;
		}

		public async Task<Order> GetOrder(string address)
		{
			await tonClient.InitIfNeeded().ConfigureAwait(false);

			var smc = await tonClient.SmcLoad(address).ConfigureAwait(false);

			var data1 = await tonClient.SmcRunGetMethod(smc.Id, new MethodIdName("get_order_data")).ConfigureAwait(false);
			if (data1.ExitCode != 0)
			{
				throw new NonZeroExitCodeException(data1.ExitCode, address, "get_order_data");
			}

			var data2 = await tonClient.SmcRunGetMethod(smc.Id, new MethodIdName("get_responses")).ConfigureAwait(false);
			if (data2.ExitCode != 0)
			{
				throw new NonZeroExitCodeException(data2.ExitCode, address, "get_responses");
			}

			await tonClient.SmcForget(smc.Id).ConfigureAwait(false);

			// Method returns:
			// (int, int, slice, int, int, int, slice, slice, cell) get_order_data()
			// (storage::init?, storage::index, storage::master_address,
			//      storage::status, storage::price, storage::deadline, storage::customer_address,
			//      storage::freelancer_address, storage::content)

			// Second method returns:
			// (cell, int) get_responses()
			// (storage::responses, storage::responses_count)

			var index = data1.Stack[1].ToLong();
			var status = data1.Stack[3].ToInt();
			var price = TonUtils.Coins.FromNano(data1.Stack[4].ToLong());
			var deadline = data1.Stack[5].ToInt();
			var customerAddress = data1.Stack[6].ToBoc().RootCells[0].BeginRead().LoadAddressIntStd(false);
			var freelancerAddress = data1.Stack[7].ToBoc().RootCells[0].BeginRead().TryLoadAddressIntStd(false);
			var content = data1.Stack[8].ToBoc();

			var responsesCount = data2.Stack[1].ToInt();

			var order = new Order()
			{
				Index = index,
				Address = TonUtils.Address.SetBounceable(address, true),
				Status = (OrderStatus)status,
				CustomerAddress = customerAddress,
				FreelancerAddress = freelancerAddress,
				CreatedAt = DateTimeOffset.UtcNow.Truncate(TimeSpan.FromSeconds(1)),
				ResponsesCount = responsesCount,
			};

			order.FillFrom(content.RootCells[0]);

			order.Price = price;
			order.Deadline = DateTimeOffset.FromUnixTimeSeconds(deadline);

			return order;
		}
	}
}
