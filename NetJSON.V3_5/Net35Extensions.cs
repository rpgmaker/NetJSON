using System;

namespace NetJSON
{
	public delegate void Action<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

	public class ExpandoObject
	{
	}

	public class Tuple<T1>
	{

		public Tuple(T1 item1) {
			Item1 = item1;
		}

		public T1 Item1 { get; set; }
	}

	public class Tuple<T1, T2>
	{

		public Tuple(T1 item1, T2 item2) {
			Item1 = item1;
			Item2 = item2;
		}

		public T1 Item1 { get; set; }
		public T2 Item2 { get; set; }
	}

	public class Tuple<T1, T2, T3>
	{

		public Tuple(T1 item1, T2 item2, T3 item3) {
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;
		}

		public T1 Item1 { get; set; }
		public T2 Item2 { get; set; }
		public T3 Item3 { get; set; }
	}

	public class Tuple<T1, T2, T3, T4>
	{

		public Tuple(T1 item1, T2 item2, T3 item3, T4 item4) {
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;
			Item4 = item4;
		}

		public T1 Item1 { get; set; }
		public T2 Item2 { get; set; }
		public T3 Item3 { get; set; }
		public T4 Item4 { get; set; }
	}

	public class Tuple<T1, T2, T3, T4, T5>
	{

		public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) {
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;
			Item4 = item4;
			Item5 = item5;
		}

		public T1 Item1 { get; set; }
		public T2 Item2 { get; set; }
		public T3 Item3 { get; set; }
		public T4 Item4 { get; set; }
		public T5 Item5 { get; set; }
	}

	public class Tuple<T1, T2, T3, T4, T5, T6>
	{

		public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) {
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;
			Item4 = item4;
			Item5 = item5;
			Item6 = item6;
		}

		public T1 Item1 { get; set; }
		public T2 Item2 { get; set; }
		public T3 Item3 { get; set; }
		public T4 Item4 { get; set; }
		public T5 Item5 { get; set; }
		public T6 Item6 { get; set; }
	}

	public class Tuple<T1, T2, T3, T4, T5, T6, T7>
	{

		public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) {
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;
			Item4 = item4;
			Item5 = item5;
			Item6 = item6;
			Item7 = item7;
		}

		public T1 Item1 { get; set; }
		public T2 Item2 { get; set; }
		public T3 Item3 { get; set; }
		public T4 Item4 { get; set; }
		public T5 Item5 { get; set; }
		public T6 Item6 { get; set; }
		public T7 Item7 { get; set; }
	}

	public class Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>
	{


		public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) {
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;
			Item4 = item4;
			Item5 = item5;
			Item6 = item6;
			Item7 = item7;
			Rest = rest;
		}

		public T1 Item1 { get; set; }
		public T2 Item2 { get; set; }
		public T3 Item3 { get; set; }
		public T4 Item4 { get; set; }
		public T5 Item5 { get; set; }
		public T6 Item6 { get; set; }
		public T7 Item7 { get; set; }
		public TRest Rest { get; set; }
	}
}
