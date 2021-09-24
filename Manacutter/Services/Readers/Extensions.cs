using Lumina;
using Manacutter.Services.Readers;
using Manacutter.Services.Readers.Lumina;

namespace Microsoft.Extensions.DependencyInjection {
	public static class ReaderServiceExtensions {
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
