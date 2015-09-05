﻿using System;
using NUnit.Framework;
using Utilities.Interfaces;
using StructureMap;
using System.IO;
using Utilities;
using System.Linq;

namespace UtilitiesTesting
{
	[TestFixture]
	[Category("ExcelUtils")]
	public class ExcelUtilsTests : BaseTest
	{
		private string _fakeExcelXlsPath;
		private string _fakeExcelXlsxPath;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			_fakeExcelXlsPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".xls";
			_fakeExcelXlsxPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".xlss";
			File.WriteAllText(_fakeExcelXlsPath, "ASD");
			File.WriteAllText(_fakeExcelXlsxPath, "ASD");
		}
		[TearDown]
		public void Dispose()
		{
			File.Delete(_fakeExcelXlsPath);
			File.Delete(_fakeExcelXlsxPath);
		}
		[Test]
		public void ConvertToCsv_PathToExcelIsNull_ExpectedArgumentNullException()
		{
			var ex = Assert.Throws<ArgumentNullException>(() => ExcelUtils.ConvertToCsv(null, "C:\\1.csv"));

			Assert.AreEqual("pathToExcel", ex.ParamName);
		}
		[Test]
		public void ConvertToCsv_PathToExcelIsEmtpy_ExpectedArgumentException()
		{
			var ex = Assert.Throws<ArgumentException>(() => ExcelUtils.ConvertToCsv(string.Empty, "C:\\1.csv"));

			Assert.AreEqual("pathToExcel", ex.ParamName);
		}
		[Test]
		public void ConvertToCsv_PathToExcelDoesntExist_ExpectedFileNotFoundException()
		{
			string pathToExcel = "C:\\" + Guid.NewGuid() + ".xls";
			var ex = Assert.Throws<FileNotFoundException>(() => ExcelUtils.ConvertToCsv(pathToExcel, "C:\\1.csv"));

			Assert.AreEqual(pathToExcel, ex.FileName);
		}
		[Test]
		public void ConvertToCsv_PathToCsvIsNull_ExpectedArgumentNullException()
		{
			var ex = Assert.Throws<ArgumentNullException>(() => ExcelUtils.ConvertToCsv(_fakeExcelXlsPath, null));

			Assert.AreEqual("pathToCsv", ex.ParamName);
		}
		[Test]
		public void ConvertToCsv_PathToCsvIsEmpty_ExpectedArgumentException()
		{
			var ex = Assert.Throws<ArgumentException>(() => ExcelUtils.ConvertToCsv(_fakeExcelXlsPath, string.Empty));

			Assert.AreEqual("pathToCsv", ex.ParamName);
		}
		[Test]
		public void ConvertToCsv_PathToExcelIsNotExcelFile_ExpectedArgumentException()
		{
			var ex = Assert.Throws<ArgumentException>(() => ExcelUtils.ConvertToCsv("C:\\1.blablabla", "C:\\1.csv"));

			Assert.AreEqual("pathToExcel", ex.ParamName);
			bool doesContain = ex.Message.Contains("Expected '.xls' or '.xlsx'");
			Assert.AreEqual(true, doesContain, "Expected '.xls' or '.xlsx'");
		}
		[Test]
		public void Functional_ConvertToCsv_1ColumnXlsx_ShouldBeOk()
		{
			string pathToCsv = Path.GetTempPath() + Guid.NewGuid().ToString() + ".csv";
			try
			{
				Assert.DoesNotThrow(() => ExcelUtils.ConvertToCsv(@"Tools\FileTools\TestFiles\1Column.xlsx", pathToCsv));

				using (ICsvReader csvReader = new CsvReader(pathToCsv))
				{
					var columns = csvReader.GetColumnHeaders();

					Assert.AreEqual(1, columns.Length, "Expected only 1 column");
					Assert.AreEqual("Column1", columns[0], "Expected column 'Column1'");
				}
			}
			finally
			{
				try { File.Delete(pathToCsv); }
				catch { }
			}
		}
		[Test]
		public void Functional_ConvertToCsv_2ColumnsXlsx_ShouldBeOk()
		{
			string pathToCsv = Path.GetTempPath() + Guid.NewGuid().ToString() + ".csv";
			try
			{
				Assert.DoesNotThrow(() => ExcelUtils.ConvertToCsv(@"Tools\FileTools\TestFiles\2Columns.xlsx", pathToCsv));

				using (ICsvReader csvReader = new CsvReader(pathToCsv))
				{
					var columns = csvReader.GetColumnHeaders();

					Assert.AreEqual(2, columns.Length, "Expected only 2 column");
					Assert.AreEqual("Column1", columns[0], "Expected column 'Column1'");
					Assert.AreEqual("Column2", columns[1], "Expected column 'Column2'");
				}
			}
			finally
			{
				try { File.Delete(pathToCsv); }
				catch { }
			}
		}
		[Test]
		public void Functional_ConvertToCsv_3ColumnsXlsx_ShouldBeOk()
		{
			string pathToCsv = Path.GetTempPath() + Guid.NewGuid().ToString() + ".csv";
			try
			{
				Assert.DoesNotThrow(() => ExcelUtils.ConvertToCsv(@"Tools\FileTools\TestFiles\3Columns.xlsx", pathToCsv));

				using (ICsvReader csvReader = new CsvReader(pathToCsv))
				{
					var columns = csvReader.GetColumnHeaders();

					Assert.AreEqual(3, columns.Length, "Expected only 3 column");
					Assert.AreEqual("Column1", columns[0], "Expected column 'Column1'");
					Assert.AreEqual("Column2", columns[1], "Expected column 'Column2'");
					Assert.AreEqual("Column3", columns[2], "Expected column 'Column3'");
				}
			}
			finally
			{
				try { File.Delete(pathToCsv); }
				catch { }
			}
		}
		[Test]
		public void Functional_ConvertToCsv_10ColumnsXlsx_ShouldBeOk()
		{
			string pathToCsv = Path.GetTempPath() + Guid.NewGuid().ToString() + ".csv";
			try
			{
				Assert.DoesNotThrow(() => ExcelUtils.ConvertToCsv(@"Tools\FileTools\TestFiles\10Columns.xlsx", pathToCsv));

				using (ICsvReader csvReader = new CsvReader(pathToCsv))
				{
					var columns = csvReader.GetColumnHeaders();

					Assert.AreEqual(10, columns.Length, "Expected only 10 column");
					Assert.AreEqual("Column1", columns[0], "Expected column 'Column1'");
					Assert.AreEqual("Column2", columns[1], "Expected column 'Column2'");
					Assert.AreEqual("Column3", columns[2], "Expected column 'Column3'");
					Assert.AreEqual("Column4", columns[3], "Expected column 'Column4'");
					Assert.AreEqual("Column5", columns[4], "Expected column 'Column5'");
					Assert.AreEqual("Column6", columns[5], "Expected column 'Column6'");
					Assert.AreEqual("Column7", columns[6], "Expected column 'Column7'");
					Assert.AreEqual("Column8", columns[7], "Expected column 'Column8'");
					Assert.AreEqual("Column9", columns[8], "Expected column 'Column9'");
					Assert.AreEqual("Column10", columns[9], "Expected column 'Column10'");
				}
			}
			finally
			{
				try { File.Delete(pathToCsv); }
				catch { }
			}
		}
		[Test]
		public void Functional_ConvertToCsv_1ColumnXls_ShouldBeOk()
		{
			string pathToCsv = Path.GetTempPath() + Guid.NewGuid().ToString() + ".csv";
			try
			{
				Assert.DoesNotThrow(() => ExcelUtils.ConvertToCsv(@"Tools\FileTools\TestFiles\1Column.xls", pathToCsv));

				using (ICsvReader csvReader = new CsvReader(pathToCsv))
				{
					var columns = csvReader.GetColumnHeaders();

					Assert.AreEqual(1, columns.Length, "Expected only 1 column");
					Assert.AreEqual("Column1", columns[0], "Expected column 'Column1'");
				}
			}
			finally
			{
				try { File.Delete(pathToCsv); }
				catch { }
			}
		}
		[Test]
		public void Functional_ConvertToCsv_2ColumnsXls_ShouldBeOk()
		{
			string pathToCsv = Path.GetTempPath() + Guid.NewGuid().ToString() + ".csv";
			try
			{
				Assert.DoesNotThrow(() => ExcelUtils.ConvertToCsv(@"Tools\FileTools\TestFiles\2Columns.xls", pathToCsv));

				using (ICsvReader csvReader = new CsvReader(pathToCsv))
				{
					var columns = csvReader.GetColumnHeaders();

					Assert.AreEqual(2, columns.Length, "Expected only 2 column");
					Assert.AreEqual("Column1", columns[0], "Expected column 'Column1'");
					Assert.AreEqual("Column2", columns[1], "Expected column 'Column2'");
				}
			}
			finally
			{
				try { File.Delete(pathToCsv); }
				catch { }
			}
		}
		[Test]
		public void Functional_ConvertToCsv_3ColumnsXls_ShouldBeOk()
		{
			string pathToCsv = Path.GetTempPath() + Guid.NewGuid().ToString() + ".csv";
			try
			{
				Assert.DoesNotThrow(() => ExcelUtils.ConvertToCsv(@"Tools\FileTools\TestFiles\3Columns.xls", pathToCsv));

				using (ICsvReader csvReader = new CsvReader(pathToCsv))
				{
					var columns = csvReader.GetColumnHeaders();

					Assert.AreEqual(3, columns.Length, "Expected only 3 column");
					Assert.AreEqual("Column1", columns[0], "Expected column 'Column1'");
					Assert.AreEqual("Column2", columns[1], "Expected column 'Column2'");
					Assert.AreEqual("Column3", columns[2], "Expected column 'Column3'");
				}
			}
			finally
			{
				try { File.Delete(pathToCsv); }
				catch { }
			}
		}
		[Test]
		public void Functional_ConvertToCsv_10ColumnsXls_ShouldBeOk()
		{
			string pathToCsv = Path.GetTempPath() + Guid.NewGuid().ToString() + ".csv";
			try
			{
				Assert.DoesNotThrow(() => ExcelUtils.ConvertToCsv(@"Tools\FileTools\TestFiles\10Columns.xls", pathToCsv));

				using (ICsvReader csvReader = new CsvReader(pathToCsv))
				{
					var columns = csvReader.GetColumnHeaders();

					Assert.AreEqual(10, columns.Length, "Expected only 10 column");
					Assert.AreEqual("Column1", columns[0], "Expected column 'Column1'");
					Assert.AreEqual("Column2", columns[1], "Expected column 'Column2'");
					Assert.AreEqual("Column3", columns[2], "Expected column 'Column3'");
					Assert.AreEqual("Column4", columns[3], "Expected column 'Column4'");
					Assert.AreEqual("Column5", columns[4], "Expected column 'Column5'");
					Assert.AreEqual("Column6", columns[5], "Expected column 'Column6'");
					Assert.AreEqual("Column7", columns[6], "Expected column 'Column7'");
					Assert.AreEqual("Column8", columns[7], "Expected column 'Column8'");
					Assert.AreEqual("Column9", columns[8], "Expected column 'Column9'");
					Assert.AreEqual("Column10", columns[9], "Expected column 'Column10'");
				}
			}
			finally
			{
				try { File.Delete(pathToCsv); }
				catch { }
			}
		}
	}
}
