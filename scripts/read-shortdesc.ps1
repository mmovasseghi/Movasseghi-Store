Add-Type -AssemblyName 'Microsoft.Data.Sqlite' -ErrorAction SilentlyContinue
$paths = @(
  'F:\Projects\Movasseghi Shop\src\MovasseghiShop.Web\movasseghi.db',
  'F:\Projects\Movasseghi Shop\src\MovasseghiShop.Web\bin\Debug\net10.0\movasseghi.db'
)
foreach ($path in $paths) {
  if (-not (Test-Path $path)) { Write-Output "$path MISSING"; continue }
  $conn = New-Object Microsoft.Data.Sqlite.SqliteConnection "Data Source=$path"
  $conn.Open()
  $cmd = $conn.CreateCommand()
  $cmd.CommandText = 'SELECT ShortDescription FROM Products WHERE Id=61'
  $val = $cmd.ExecuteScalar()
  Write-Output "$path => $val"
  $conn.Close()
}
