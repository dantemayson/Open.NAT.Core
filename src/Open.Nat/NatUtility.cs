//
// Authors:
//   Ben Motmans <ben.motmans@gmail.com>
//   Lucas Ontivero lucasontivero@gmail.com
//
// Copyright (C) 2007 Ben Motmans
// Copyright (C) 2014 Lucas Ontivero
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Open.Nat
{
	public static class NatUtility
	{
		public static event EventHandler<DeviceEventArgs> DeviceFound;
        public static event EventHandler<UnhandledExceptionEventArgs> UnhandledException;

        private static readonly ManualResetEvent Searching;
        internal static List<ISearcher> Searchers = new List<ISearcher>
        {
            new UpnpSearcher(new IPAddressesProvider()), 
            new PmpSearcher(new IPAddressesProvider())
        };

	    public static TextWriter Logger { get; set; }
	    public static bool Verbose { get; set; }

	    static NatUtility()
        {
            Searching = new ManualResetEvent(false);
        }

        public static void Initialize()
        {
            foreach (var searcher in Searchers)
            {
                searcher.DeviceFound += OnDeviceFound;
            }

            Task.Factory.StartNew(SearchAndListen, TaskCreationOptions.LongRunning);
        }

        private static void SearchAndListen()
        {
            while (true)
            {
                Searching.WaitOne();

                try
                {
                    foreach (var searcher in Searchers)
                    {
                        searcher.Receive();
                    }

                    foreach (var searcher in Searchers)
                    {
                        searcher.Search();
                    }
                }
                catch (Exception e)
                {
                    if (UnhandledException != null)
                        UnhandledException(typeof(NatUtility), new UnhandledExceptionEventArgs(e, false));
                }
                Thread.Sleep(10);
            }
        }

	    private static void OnDeviceFound(object sender, DeviceEventArgs args)
	    {
	        var handler = DeviceFound;
            if (handler != null) handler(sender, args);
	    }

	    internal static void Log(string format, params object[] args)
		{
			var logger = Logger;
			if (logger != null) logger.WriteLine(format, args);
		}

		public static void StartDiscovery ()
		{
            Searching.Set();
		}

		public static void StopDiscovery ()
		{
            Searching.Reset();
		}
	}
}