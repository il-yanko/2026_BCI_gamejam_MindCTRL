// Stub for the Lab Streaming Layer (LSL) API.
// Provides minimal implementations so bci-essentials-unity compiles without
// the real LSL4Unity package.  Replace this by installing LSL4Unity when you
// are ready to connect to actual BCI hardware.

namespace LSL
{
    public enum channel_format_t
    {
        cf_undefined = 0,
        cf_float32 = 1,
        cf_double64 = 2,
        cf_string = 3,
        cf_int32 = 4,
        cf_int16 = 5,
        cf_int8 = 6,
        cf_int64 = 7,
    }

    public class StreamInfo
    {
        public StreamInfo() { }
        public StreamInfo(
            string name, string type,
            int channel_count = 1,
            double nominal_srate = 0,
            channel_format_t channel_format = channel_format_t.cf_string,
            string source_id = "")
        { }

        public int channel_count() => 0;
        public string name() => string.Empty;
        public string type() => string.Empty;
    }

    public class StreamInlet : System.IDisposable
    {
        public StreamInlet(StreamInfo info) { }
        public int samples_available() => 0;
        public void open_stream(double timeout = 32.0) { }
        public void close_stream() { }
        public void Dispose() { }
        public double pull_sample(string[] sample, double timeout = 0.0) => 0.0;
    }

    public class StreamOutlet : System.IDisposable
    {
        public StreamOutlet(StreamInfo info) { }
        public bool have_consumers() => false;
        public void wait_for_consumers(double timeout = 32.0) { }
        public void push_sample(string[] data) { }
        public void Close() { }
        public void Dispose() { }
    }

    public class StreamResolver { }

    // Static helper — matches "using static LSL.LSL;" in the submodule.
    public static class LSL
    {
        // Predicate-based overload: resolve_stream("type='XYZ'", 1, 0)
        public static StreamInfo[] resolve_stream(
            string predicate, int minimum = 1, double timeout = 0)
            => new StreamInfo[0];

        // Property/value overload: resolve_stream("name", "MyStream", 1, 1)
        public static StreamInfo[] resolve_stream(
            string prop, string value, int minimum = 1, double timeout = 0)
            => new StreamInfo[0];
    }
}
