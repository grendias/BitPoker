﻿using System;
using System.IO;
using System.Text;

namespace BitPoker.ETH.Console
{
	class MainClass
	{
		private static String CONTRACT_PATH = @"/Users/lucascullen/GitHub/BitcoinBrisbane/BitPoker/bin/BitPoker.ETH.Contracts/";
		private static String CONTRACT_FILE_NAME = "Cashier";

		public static void Main(string[] args)
		{
			//Connect to Geth node
			Nethereum.Web3.Web3 web3 = new Nethereum.Web3.Web3();
			var password = "Test";

			var accounts = web3.Personal.ListAccounts.SendRequestAsync().Result;

			for (Int32 i = 0; i < accounts.Length; i++)
			{
				var balance = web3.Eth.GetBalance.SendRequestAsync(accounts[i]).Result;
				System.Console.WriteLine(accounts[i] + " " + balance.Value);
			}

			Boolean unlockResponse = web3.Personal.UnlockAccount.SendRequestAsync(accounts[0], password, 120).Result;

			//Contract
			var bytes = GetBytesFromFile(CONTRACT_PATH + CONTRACT_FILE_NAME + ".bin"); 

			//Deploy the contract

			String contractHash = web3.Eth.DeployContract.SendRequestAsync(bytes, accounts[0], new Nethereum.Hex.HexTypes.HexBigInteger(1000000)).Result;
			System.Console.WriteLine("Contract hash {0}", contractHash);
			
			//Write out the response from the contract
			var isMining = web3.Eth.Mining.IsMining.SendRequestAsync().Result;

			if (isMining != true)
			{
				var mResult = web3.Miner.Start.SendRequestAsync().Result;
			}

			var receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(contractHash).Result;
			System.Console.WriteLine("Contract receipt {0}", receipt.BlockHash);
			
			System.Console.Write("Processing");

			while (receipt == null)
			{
				System.Threading.Thread.Sleep(3000);
				receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(contractHash).Result;
				System.Console.Write(".");
			}

			if (receipt != null)
			{
				System.Console.WriteLine(receipt.BlockHash);
			}

			var abi = GetABIFromFile(CONTRACT_PATH + CONTRACT_FILE_NAME + ".abi");

			var contractAddress = receipt.ContractAddress;
			var contract = web3.Eth.GetContract(abi, contractAddress);
			var buyFunction = contract.GetFunction("buy");

			
			unlockResponse = web3.Personal.UnlockAccount.SendRequestAsync(accounts[1], password, 120).Result;

			if (unlockResponse == true)
			{
				var estimatedTx = buyFunction.SendTransactionAsync(accounts[1]).Result;
				System.Console.WriteLine(estimatedTx);
				receipt = null;

				while (receipt == null)
				{
					System.Threading.Thread.Sleep(3000);
					receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(estimatedTx).Result;
					System.Console.Write(".");
				}
			}

			var individualReturns = contract.GetFunction("individualReturns");
		}

		private static string GetABIFromFile(String path)
		{
			string abi = File.ReadAllText(path, Encoding.UTF8);
			return abi;
		}

		private static string GetBytesFromFile(String path)
		{
			var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
			using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
			{
				String text = streamReader.ReadToEnd();
				return "0x" + text;
			}
		}
	}
}