<#
    Generar_Catalogo_Pruebas.ps1
    ----------------------------
    Genera el catalogo de pruebas de un proyecto de pruebas a partir del archivo .trx de la
    ejecucion y de su Catalogo_Descripciones.txt.

    Se invoca solo al terminar `dotnet test` (target GenerarCatalogoPruebas de
    tests/Directory.Build.targets). Tambien se puede ejecutar a mano:

        .\Generar_Catalogo_Pruebas.ps1 -Proyecto .\MundialitoCorporativo.UnitTests\MundialitoCorporativo.UnitTests.csproj
        .\Generar_Catalogo_Pruebas.ps1 -Proyecto ... -SinEjecutar     (usa el ultimo .trx)
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Proyecto,
    [string] $Trx,
    [switch] $SinEjecutar,
    [string] $Catalogo,
    [string] $Salida,
    [string] $Titulo    = 'CATALOGO DE PRUEBAS',
    [string] $Requisito = 'Requerimiento: item 15 - Pruebas',
    [string] $Entorno   = '',
    # Lineas de alcance separadas por el caracter |
    [string] $Alcance   = ''
)

$ErrorActionPreference = 'Stop'
$carpeta = Split-Path $Proyecto -Parent
if (-not $Catalogo) { $Catalogo = Join-Path $carpeta 'Catalogo_Descripciones.txt' }
if (-not $Salida)   { $Salida   = Join-Path $carpeta 'Catalogo_Pruebas.txt' }
$resultados = Join-Path $carpeta 'ResultadosDePrueba'

# -- 1. Obtener el .trx -------------------------------------------------------
if ($Trx) {
    $archivoTrx = $Trx
}
elseif ($SinEjecutar) {
    $archivoTrx = Get-ChildItem $resultados -Filter '*.trx' -ErrorAction SilentlyContinue |
                  Sort-Object LastWriteTime -Descending |
                  Select-Object -First 1 -ExpandProperty FullName
    if (-not $archivoTrx) { throw "No hay ningun .trx previo en $resultados. Ejecute sin -SinEjecutar." }
}
else {
    Write-Host 'Ejecutando las pruebas...' -ForegroundColor Cyan
    $nombre = 'Catalogo_{0}.trx' -f (Get-Date -Format 'yyyy-MM-dd_HHmmss')
    # Sin el target automatico, para no generar el catalogo dos veces.
    dotnet test $Proyecto --nologo --logger "trx;LogFileName=$nombre" --results-directory $resultados -p:GenerarCatalogoPruebas=false | Out-Host
    $archivoTrx = Join-Path $resultados $nombre
}
if (-not (Test-Path $archivoTrx)) { throw "No se encontro el archivo de resultados $archivoTrx." }

# -- 2. Leer el catalogo de descripciones ------------------------------------
if (-not (Test-Path $Catalogo)) { throw "Falta el catalogo de descripciones: $Catalogo" }

$descripciones = @{}
foreach ($fila in Get-Content $Catalogo -Encoding UTF8) {
    $t = $fila.Trim()
    if ($t.Length -eq 0 -or $t.StartsWith('#')) { continue }
    $p = $t -split '\|'
    if ($p.Count -lt 4) { continue }
    $descripciones["$($p[0].Trim()).$($p[1].Trim())"] = [pscustomobject]@{
        Componente = $p[2].Trim()
        Objetivo   = $p[3].Trim()
    }
}

# -- 3. Leer el .trx ----------------------------------------------------------
[xml]$xml = Get-Content $archivoTrx -Raw -Encoding UTF8

$definiciones = @{}
foreach ($ut in $xml.TestRun.TestDefinitions.UnitTest) {
    $clase = ($ut.TestMethod.className -split ',')[0]
    $definiciones[$ut.id] = [pscustomobject]@{
        ClaseCorta = ($clase -split '\.')[-1]
        Metodo     = $ut.TestMethod.name
    }
}

$casos = foreach ($r in $xml.TestRun.Results.UnitTestResult) {
    $d = $definiciones[$r.testId]
    if (-not $d) { continue }

    # El nombre mostrado incluye los datos de la variante entre parentesis.
    $variante = ''
    $i = $r.testName.IndexOf('(')
    if ($i -ge 0) { $variante = $r.testName.Substring($i) }

    $ms = 0
    if ($r.duration) {
        try { $ms = [math]::Round(([TimeSpan]::Parse($r.duration)).TotalMilliseconds) } catch { $ms = 0 }
    }

    $estado = switch ($r.outcome) { 'Passed' { 'Aprobada' } 'Failed' { 'Fallida' } default { 'Omitida' } }
    $mensaje = $null
    if ($r.Output -and $r.Output.ErrorInfo) { $mensaje = $r.Output.ErrorInfo.Message }
    if (-not $mensaje -and $r.Output -and $r.Output.StdOut) { $mensaje = $r.Output.StdOut }

    [pscustomobject]@{
        Clase    = $d.ClaseCorta
        Metodo   = $d.Metodo
        Variante = $variante
        Estado   = $estado
        Ms       = $ms
        Mensaje  = $mensaje
    }
}

if (-not $casos) { throw 'El archivo de resultados no contiene ninguna prueba.' }

function Contar($lista, $estado) { ($lista | Where-Object Estado -eq $estado | Measure-Object).Count }

$total    = ($casos | Measure-Object).Count
$aprob    = Contar $casos 'Aprobada'
$fallidas = Contar $casos 'Fallida'
$omitidas = Contar $casos 'Omitida'

function EstadoGrupo($a, $f, $o) {
    if ($f -gt 0) { 'CON FALLOS' }
    elseif ($o -gt 0) { 'APROBADO (con omitidas)' }
    else { 'APROBADO' }
}

# -- 4. Componer el informe ---------------------------------------------------
$sb = [System.Text.StringBuilder]::new()
function Esc($t) { [void]$sb.AppendLine($t) }
$SEP1 = '=' * 100
$SEP2 = '-' * 100

$fechaEjec = $xml.TestRun.Times.finish
try { $fechaEjec = ([datetime]$fechaEjec).ToString('yyyy-MM-dd HH:mm:ss') } catch { }

Esc $SEP1
Esc ('  ' + $Titulo)
Esc  '  Proyecto: Mundialito de Futbol Corporativo (API .NET 10 . Clean Architecture . CQRS)'
Esc ('  ' + $Requisito)
Esc $SEP1
Esc ''
Esc ("  Fecha de ejecucion : {0}" -f $fechaEjec)
Esc ("  Generado           : {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
Esc  '  Marco de pruebas   : xUnit 2.9.3 . Microsoft.NET.Test.Sdk 17.14.1 . .NET 10'
if ($Entorno) { Esc ("  Entorno            : {0}" -f $Entorno) }
Esc ("  Archivo de origen  : {0}" -f (Split-Path $archivoTrx -Leaf))
Esc ''
if ($fallidas -eq 0 -and $omitidas -eq 0) {
    Esc ("  RESULTADO GLOBAL   : {0} de {0} APROBADAS   (0 fallidas, 0 omitidas)" -f $total)
} else {
    Esc ("  RESULTADO GLOBAL   : {0} aprobadas de {1}   ({2} fallidas, {3} omitidas)" -f $aprob, $total, $fallidas, $omitidas)
}
Esc ''
Esc $SEP1
Esc '  RESUMEN POR GRUPO'
Esc $SEP2
Esc ('  {0,-38} {1,6} {2,10} {3,9} {4,9}   {5}' -f 'Grupo', 'Casos', 'Aprobadas', 'Fallidas', 'Omitidas', 'Estado')
Esc $SEP2

$grupos = $casos | Group-Object Clase | Sort-Object @{ Expression = { $_.Count }; Descending = $true }, Name
foreach ($g in $grupos) {
    $gA = Contar $g.Group 'Aprobada'; $gF = Contar $g.Group 'Fallida'; $gO = Contar $g.Group 'Omitida'
    Esc ('  {0,-38} {1,6} {2,10} {3,9} {4,9}   {5}' -f $g.Name, $g.Count, $gA, $gF, $gO, (EstadoGrupo $gA $gF $gO))
}
Esc $SEP2
Esc ('  {0,-38} {1,6} {2,10} {3,9} {4,9}' -f 'TOTAL', $total, $aprob, $fallidas, $omitidas)
Esc ''
Esc '  Nota: los metodos marcados [Theory] se ejecutan una vez por cada juego de datos,'
Esc '        por eso el numero de casos es mayor que el de metodos de prueba.'
Esc ''

# -- 5. Detalle por grupo y por prueba ---------------------------------------
$sinDescripcion = New-Object System.Collections.Generic.List[string]
$n = 0
foreach ($g in $grupos) {
    $n++
    $gA = Contar $g.Group 'Aprobada'; $gF = Contar $g.Group 'Fallida'; $gO = Contar $g.Group 'Omitida'

    Esc ''
    Esc $SEP1
    Esc ('  {0}. {1}' -f $n, $g.Name)
    Esc ('     {0} casos  -  {1} aprobadas  -  {2} fallidas  -  {3} omitidas  -  {4}' -f $g.Count, $gA, $gF, $gO, (EstadoGrupo $gA $gF $gO))
    Esc $SEP1
    Esc ''

    foreach ($m in ($g.Group | Group-Object Metodo | Sort-Object Name)) {
        $clave = "$($g.Name).$($m.Name)"
        $d = $descripciones[$clave]
        if (-not $d) {
            $sinDescripcion.Add($clave)
            $d = [pscustomobject]@{ Componente = '(sin registrar)'; Objetivo = 'SIN DESCRIPCION - agregar la linea correspondiente en Catalogo_Descripciones.txt' }
        }

        $mF = Contar $m.Group 'Fallida'; $mO = Contar $m.Group 'Omitida'
        $etiqueta = if ($mF -gt 0) { '[ FALLIDA ]' } elseif ($mO -eq $m.Count) { '[ OMITIDA ]' } else { '[APROBADA]' }

        Esc ("  {0}  {1}" -f $etiqueta, $m.Name)
        Esc ("      Prueba a  : {0}" -f $d.Componente)

        # El objetivo se reparte en lineas de 88 caracteres para que se lea en el Bloc de notas.
        $buffer = ''
        $primera = $true
        foreach ($p in ($d.Objetivo -split '\s+')) {
            if (($buffer.Length + $p.Length + 1) -gt 88) {
                Esc ("      {0}{1}" -f $(if ($primera) { 'Objetivo  : ' } else { '            ' }), $buffer)
                $primera = $false
                $buffer = $p
            } else {
                $buffer = if ($buffer) { "$buffer $p" } else { $p }
            }
        }
        if ($buffer) { Esc ("      {0}{1}" -f $(if ($primera) { 'Objetivo  : ' } else { '            ' }), $buffer) }

        if ($m.Count -eq 1) {
            Esc ("      Resultado : {0}   ({1} ms)" -f $m.Group[0].Estado.ToUpper(), $m.Group[0].Ms)
        } else {
            Esc ("      Resultado : {0} variantes  -  {1} aprobadas  -  {2} fallidas  -  {3} omitidas" -f $m.Count, ($m.Count - $mF - $mO), $mF, $mO)
            foreach ($c in ($m.Group | Sort-Object Variante)) {
                $v = if ($c.Variante) { $c.Variante } else { '(sin datos)' }
                if ($v.Length -gt 60) { $v = $v.Substring(0, 57) + '...' }
                Esc ("                  {0,-62} {1,-8}  ({2} ms)" -f $v, $c.Estado.ToUpper(), $c.Ms)
            }
        }

        foreach ($c in ($m.Group | Where-Object Estado -eq 'Fallida')) {
            Esc ("      MOTIVO DEL FALLO   : {0}" -f ($c.Mensaje -replace '\s+', ' '))
        }
        foreach ($c in ($m.Group | Where-Object { $_.Estado -eq 'Omitida' -and $_.Mensaje })) {
            Esc ("      MOTIVO DE OMISION  : {0}" -f ($c.Mensaje -replace '\s+', ' '))
        }
        Esc ''
    }
}

# -- 6. Cierre ----------------------------------------------------------------
Esc $SEP1
Esc '  NOTAS'
Esc $SEP2
Esc '  - El estado de cada prueba se toma del archivo .trx que produce la propia ejecucion.'
Esc '    Este informe no se edita a mano: se regenera en cada `dotnet test`.'
Esc ('  - Los objetivos y el componente probado provienen de ' + (Split-Path $Catalogo -Leaf) + ', que se')
Esc '    mantiene junto a las pruebas. Una prueba nueva sin su linea aparece como SIN DESCRIPCION.'
if ($Alcance) { foreach ($l in ($Alcance -split '\|')) { Esc ('  ' + $l) } }
if ($sinDescripcion.Count -gt 0) {
    Esc ''
    Esc ('  ATENCION: {0} prueba(s) sin descripcion en el catalogo:' -f $sinDescripcion.Count)
    foreach ($s in $sinDescripcion) { Esc "     - $s" }
}
Esc $SEP1

# -- 7. Escribir --------------------------------------------------------------
# UTF-8 con BOM para que el Bloc de notas y Excel muestren bien los acentos.
[System.IO.File]::WriteAllText($Salida, $sb.ToString(), [System.Text.UTF8Encoding]::new($true))

$resumen = if ($fallidas -eq 0) { "{0} de {1} aprobadas, {2} omitidas" -f $aprob, $total, $omitidas } else { "{0} aprobadas de {1} ({2} fallidas)" -f $aprob, $total, $fallidas }
Write-Host ("  Catalogo de pruebas: {0} -> {1}" -f $resumen, $Salida)
if ($sinDescripcion.Count -gt 0) {
    Write-Host ("  ATENCION: {0} prueba(s) sin descripcion en el catalogo." -f $sinDescripcion.Count)
}
