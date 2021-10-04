using Lumina;
using Manacutter.Readers;
using Manacutter.Readers.Lumina;

namespace Microsoft.Extensions.DependencyInjection {
	public static class ReadersExtensions {
		public static IServiceCollection AddReaders(this IServiceCollection services) {
			// TODO: arguably the lumina reader should do this internally
			// TODO: Configurable data location
			var lumina = new GameData("C:\\Program Files (x86)\\SquareEnix\\FINAL FANTASY XIV - A Realm Reborn\\game\\sqpack");
			services.AddSingleton(lumina);

			services.AddSingleton<IReader, LuminaReader>();

			return services;
		}
	}
}
