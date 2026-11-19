namespace LibraryManager.Domain.Catalog
{
    /// <summary>
    /// Nombres ficticios para generar el catálogo. Cambiar el orden o el contenido cambia
    /// los catálogos generados con una semilla: las partidas guardan su catálogo, así que
    /// solo afecta a partidas nuevas.
    /// </summary>
    internal static class CatalogNameBank
    {
        public static readonly string[] Distributors =
        {
            "Distribuciones Atalaya",
            "Cauce Distribuidora",
            "Logística del Libro Norte",
            "Grupo Arcadia Distribución"
        };

        public static readonly string[] Publishers =
        {
            "Ediciones Faro Alto", "Editorial Brújula", "Grupo Editorial Meridiano", "Alfaguarda Libros",
            "Ediciones del Viento Sur", "Editorial Candela", "Libros del Mirlo", "Ediciones Escarcha",
            "Editorial Cuarto Creciente", "Ediciones La Hoja Roja", "Editorial Piedra Lunar", "Ediciones Trébol",
            "Libros de la Ballena", "Editorial Granate", "Ediciones Lince", "Editorial Vela Latina",
            "Ediciones El Desván", "Pequeña Editorial Azafrán", "Ediciones Sextante", "Editorial Polilla"
        };

        public static readonly string[] FirstNames =
        {
            "Lucía", "Martín", "Carmen", "Javier", "Elena", "Pablo", "Marta", "Andrés", "Irene", "Tomás",
            "Sara", "Diego", "Nuria", "Álvaro", "Inés", "Rodrigo", "Laura", "Hugo", "Clara", "Bruno",
            "Paula", "Daniel", "Alicia", "Óscar", "Rocío", "Gabriel", "Teresa", "Ignacio", "Julia", "Mateo",
            "Beatriz", "Samuel", "Olga", "Raúl", "Aitana", "Fermín", "Celia", "Gonzalo", "Noelia", "Emilio",
            "Ana", "Lorenzo", "Silvia", "Víctor", "Amparo", "Héctor", "Lidia", "Joaquín", "Mireia", "Iker"
        };

        public static readonly string[] LastNames =
        {
            "Alcázar", "Briones", "Cuesta", "Dávila", "Echeverri", "Fuster", "Galván", "Hidalgo", "Iriarte", "Jurado",
            "Lozano", "Maldonado", "Noriega", "Olmedo", "Pardo", "Quintana", "Rivas", "Salcedo", "Tejedor", "Urquiza",
            "Valcárcel", "Zamora", "Arroyo", "Bermejo", "Campos", "Del Río", "Escobar", "Ferrán", "Garrido", "Huerta",
            "Ibarra", "Linares", "Medina", "Navarrete", "Ortuño", "Peralta", "Roldán", "Segura", "Toledo", "Villalba",
            "Aranda", "Bustos", "Carvajal", "Duarte", "Espinosa", "Fajardo", "Gil de Biedma", "Llorente", "Montero", "Vidal"
        };

        // Sustantivos con artículo, para títulos de ficción: «El faro de la niebla».
        public static readonly string[] Nouns =
        {
            "El faro", "La marea", "El silencio", "La casa", "El jardín", "La sombra", "El invierno", "La herida",
            "El puerto", "La ciudad", "El último verano", "La memoria", "El espejo", "La noche", "El río", "La frontera",
            "El secreto", "La isla", "El viaje", "La carta", "El reloj", "La llave", "El bosque", "La tormenta",
            "El hijo", "La hija", "El cartógrafo", "La relojera", "El guardián", "La viajera", "El ruido", "La luz"
        };

        public static readonly string[] Complements =
        {
            "de la niebla", "del norte", "de las mareas", "de los almendros", "del olvido", "de la sal",
            "de los nombres", "del alba", "de las horas", "de cristal", "del lobo", "de la ceniza",
            "del faro", "de los vencidos", "de la abuela", "de agosto", "de las islas", "del bosque",
            "de nadie", "de papel", "del sur", "de la lluvia", "de los ríos", "sin nombre"
        };

        public static readonly string[] CrimeTitles =
        {
            "Muerte en {0}", "El crimen de {0}", "Nadie duerme en {0}", "Sangre en {0}", "Los muertos de {0}",
            "Expediente {0}", "La desaparecida de {0}", "Silencio en {0}"
        };

        public static readonly string[] Places =
        {
            "Salinas", "Puerto Viejo", "Valdecierva", "la calle Ancha", "Mirasierra", "el barrio alto",
            "Castroverde", "la estación", "Villanueva del Mar", "el hotel Imperial", "Peñarroja", "la dársena"
        };

        public static readonly string[] FantasyTitles =
        {
            "La corona de {0}", "Los hijos de {0}", "El trono de {0}", "Crónicas de {0}", "El dragón de {0}",
            "La espada de {0}", "Los reinos de {0}", "La bruja de {0}"
        };

        public static readonly string[] FantasyNames =
        {
            "Eldoria", "Varkas", "Ithiel", "Morvane", "Aldrennia", "Kael", "Sylvaran", "Tor Mhar", "Nimrel", "Ostrava"
        };

        public static readonly string[] SciFiTitles =
        {
            "Órbita {0}", "Los colonos de {0}", "Protocolo {0}", "Señales desde {0}", "La última estación de {0}",
            "Proyecto {0}", "Deriva {0}", "Los androides de {0}"
        };

        public static readonly string[] SciFiNames =
        {
            "Kepler", "Ganímedes", "Titán", "Andrómeda", "Europa", "Proxima", "Vega", "Helios", "Cerberus", "Altair"
        };

        public static readonly string[] RomanceTitles =
        {
            "Un verano en {0}", "Te esperaré en {0}", "Cartas desde {0}", "Todo empezó en {0}",
            "Volver a {0}", "Lo que pasó en {0}", "Bajo el cielo de {0}", "Una librería en {0}"
        };

        public static readonly string[] RomancePlaces =
        {
            "Lisboa", "la Toscana", "Edimburgo", "Cádiz", "Viena", "Mallorca", "París", "la Provenza", "Oporto", "Galway"
        };

        public static readonly string[] ChildrenTitles =
        {
            "{0} y el dragón despistado", "{0} no quiere dormir", "El gran viaje de {0}", "{0} va al colegio",
            "{0} y la luna", "Un día con {0}", "{0} busca un amigo", "El cumpleaños de {0}"
        };

        public static readonly string[] ChildrenCharacters =
        {
            "Pipo", "La osa Lola", "Ratón Martín", "Nube", "El pingüino Tito", "Lía", "Bruno el castor", "Pecas", "Mimi", "Trasto"
        };

        public static readonly string[] YoungAdultTitles =
        {
            "Lo que nunca te dije", "Todos los colores del cielo", "Diecisiete", "La chica del faro", "Antes de irnos",
            "El instituto de las sombras", "Cuando éramos invencibles", "Los que no se rinden", "Mil maneras de volver",
            "La lista de Alba", "Nadie como nosotros", "El verano que lo cambió todo"
        };

        public static readonly string[] ComicTitles =
        {
            "Capitana {0}", "{0}: la serie", "Las aventuras de {0}", "{0} contra el mundo", "Patrulla {0}", "{0} vol."
        };

        public static readonly string[] ComicNames =
        {
            "Tormenta", "Zarpa", "Neón", "Relámpago", "Garra", "Cometa", "Sombra", "Chispa", "Titanio", "Ráfaga"
        };

        public static readonly string[] EssayTitles =
        {
            "Breve historia de {0}", "Elogio de {0}", "Contra {0}", "La era de {0}", "Pensar {0}", "El siglo de {0}",
            "Una historia de {0}", "Lo que sabemos de {0}"
        };

        public static readonly string[] EssayTopics =
        {
            "la lectura", "la lentitud", "las ciudades", "la memoria", "el mar", "la ciencia", "el trabajo",
            "la imprenta", "los bosques", "la música", "el clima", "la amistad", "el dinero", "la atención"
        };

        public static readonly string[] SelfHelpTitles =
        {
            "El arte de {0}", "Aprende a {0}", "{0} sin miedo", "Siete pasos para {0}", "El poder de {0}", "Atrévete a {0}"
        };

        public static readonly string[] SelfHelpTopics =
        {
            "decir no", "empezar de nuevo", "respirar", "ordenar tu vida", "dormir bien", "escuchar",
            "cambiar de hábitos", "vivir despacio", "confiar", "concentrarte"
        };

        public static readonly string[] CookingTitles =
        {
            "Cocina de {0}", "Recetas de {0}", "Sabores de {0}", "La mesa de {0}", "{0} en casa"
        };

        public static readonly string[] CookingTopics =
        {
            "la abuela", "temporada", "mercado", "aprovechamiento", "Asturias", "Japón", "Marruecos",
            "cuchara", "verano", "Italia", "Perú", "horno"
        };

        public static readonly string[] TravelTitles =
        {
            "Guía de {0}", "{0} a pie", "Rutas por {0}", "{0} en una semana", "Descubrir {0}"
        };

        public static readonly string[] TravelPlaces =
        {
            "Galicia", "Portugal", "Islandia", "los Pirineos", "Andalucía", "Escocia", "Japón", "Roma",
            "Asturias", "Marruecos", "el Camino de Santiago", "Noruega"
        };

        public static readonly string[] PoetryTitles =
        {
            "Pájaros de {0}", "Cuaderno de {0}", "Canciones de {0}", "Poemas de {0}", "Diario de {0}", "Elegía de {0}"
        };

        public static readonly string[] SchoolSubjects =
        {
            "Lengua castellana", "Matemáticas", "Ciencias de la naturaleza", "Ciencias sociales", "Inglés",
            "Geografía e historia", "Física y química", "Biología", "Lengua y literatura", "Plástica"
        };

        public static readonly string[] SchoolCourses =
        {
            "1.º Primaria", "2.º Primaria", "3.º Primaria", "4.º Primaria", "5.º Primaria", "6.º Primaria",
            "1.º ESO", "2.º ESO", "3.º ESO", "4.º ESO", "1.º Bachillerato", "2.º Bachillerato"
        };
    }
}
