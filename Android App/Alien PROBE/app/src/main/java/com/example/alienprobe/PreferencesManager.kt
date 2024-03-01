import android.content.Context
import android.content.SharedPreferences

class PreferencesManager(context: Context) {
    private val sharedPreferences: SharedPreferences = context.getSharedPreferences("AppPreferences", Context.MODE_PRIVATE)

    fun savePreferences(appPreferences: AppPreferences) {
        with(sharedPreferences.edit()) {
            putString("IP", appPreferences.ip)
            putInt("Port", appPreferences.port)
            putString("Username", appPreferences.username)
            putString("Password", appPreferences.password)
            apply()
        }
    }

    fun loadPreferences(): AppPreferences {
        return AppPreferences(
            ip = sharedPreferences.getString("IP", "") ?: "",
            port = sharedPreferences.getInt("Port", 0),
            username = sharedPreferences.getString("Username", "") ?: "",
            password = sharedPreferences.getString("Password", "") ?: ""
        )
    }
}

data class AppPreferences(
    val ip: String,
    val port: Int,
    val username: String,
    val password: String
)