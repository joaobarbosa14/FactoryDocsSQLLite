Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Net.Http
Imports System.Globalization
Imports System.Net.Http.Json
Imports Microsoft.VisualBasic.FileIO

Public Class ImportRow
    Public Property DocumentNumber As String = ""
    Public Property Title As String = ""
    Public Property Category As String = ""
    Public Property Description As String = ""
    Public Property Status As String = "Draft"
End Class

Module Program
    Private ReadOnly AllowedStatuses As String() = {"Draft", "PendingApproval", "Approved", "Rejected", "Archived"}

    Async Function Main(args As String()) As Task
        If args.Length < 2 Then
            Console.WriteLine("Usage: dotnet run -- <csv-path> <api-base-url>")
            Console.WriteLine("Example: dotnet run -- ../../samples/sample-documents.csv http://localhost:5000")
            Environment.ExitCode = 2
            Return
        End If

        Dim csvPath = args(0)
        Dim apiBaseUrl = args(1).TrimEnd("/"c)

        If Not IO.File.Exists(csvPath) Then
            Console.Error.WriteLine($"CSV file not found: {csvPath}")
            Environment.ExitCode = 2
            Return
        End If

        Dim imported = 0
        Dim failed = 0
        Dim rowNumber = 1

        Using client As New HttpClient()
            client.Timeout = TimeSpan.FromSeconds(15)

            Using parser As New TextFieldParser(csvPath)
                parser.TextFieldType = FieldType.Delimited
                parser.SetDelimiters(",")
                parser.HasFieldsEnclosedInQuotes = True

                If parser.EndOfData Then
                    Console.WriteLine("CSV file is empty.")
                    Return
                End If

                Dim headers = parser.ReadFields()
                If headers Is Nothing Then
                    Console.WriteLine("Could not read CSV header.")
                    Environment.ExitCode = 2
                    Return
                End If

                Dim headerMap = headers.Select(Function(h, i) New With {.Name = h.Trim(), .Index = i}).
                    ToDictionary(Function(x) x.Name, Function(x) x.Index, StringComparer.OrdinalIgnoreCase)

                Dim requiredHeaders = {"DocumentNumber", "Title", "Category", "Description", "Status"}
                Dim missing = requiredHeaders.Where(Function(h) Not headerMap.ContainsKey(h)).ToList()
                If missing.Count > 0 Then
                    Console.Error.WriteLine("Missing CSV columns: " & String.Join(", ", missing))
                    Environment.ExitCode = 2
                    Return
                End If

                While Not parser.EndOfData
                    rowNumber += 1
                    Dim fields As String() = Nothing

                    Try
                        fields = parser.ReadFields()
                        If fields Is Nothing Then Continue While

                        Dim item As New ImportRow With {
                            .DocumentNumber = GetField(fields, headerMap("DocumentNumber")),
                            .Title = GetField(fields, headerMap("Title")),
                            .Category = GetField(fields, headerMap("Category")),
                            .Description = GetField(fields, headerMap("Description")),
                            .Status = GetField(fields, headerMap("Status"))
                        }

                        Dim validationError = Validate(item)
                        If validationError IsNot Nothing Then
                            failed += 1
                            Console.WriteLine($"Row {rowNumber}: FAILED validation — {validationError}")
                            Continue While
                        End If

                        Dim response = Await client.PostAsJsonAsync($"{apiBaseUrl}/api/documents", item)
                        If response.IsSuccessStatusCode Then
                            imported += 1
                            Console.WriteLine($"Row {rowNumber}: IMPORTED — {item.DocumentNumber}")
                        Else
                            failed += 1
                            Dim body = Await response.Content.ReadAsStringAsync()
                            Console.WriteLine($"Row {rowNumber}: FAILED API ({CInt(response.StatusCode)}) — {item.DocumentNumber}: {body}")
                        End If
                    Catch ex As Exception
                        failed += 1
                        Console.WriteLine($"Row {rowNumber}: FAILED — {ex.Message}")
                    End Try
                End While
            End Using
        End Using

        Console.WriteLine()
        Console.WriteLine("Import summary")
        Console.WriteLine($"Imported: {imported}")
        Console.WriteLine($"Failed:   {failed}")
        Console.WriteLine($"Total:    {imported + failed}")
        If failed > 0 Then Environment.ExitCode = 1
    End Function

    Private Function GetField(fields As String(), index As Integer) As String
        If index < 0 OrElse index >= fields.Length Then Return ""
        Return If(fields(index), "").Trim()
    End Function

    Private Function Validate(item As ImportRow) As String
        If String.IsNullOrWhiteSpace(item.DocumentNumber) Then Return "DocumentNumber is required."
        If String.IsNullOrWhiteSpace(item.Title) Then Return "Title is required."
        If String.IsNullOrWhiteSpace(item.Category) Then Return "Category is required."
        If String.IsNullOrWhiteSpace(item.Status) Then item.Status = "Draft"
        If Not AllowedStatuses.Contains(item.Status, StringComparer.OrdinalIgnoreCase) Then
            Return "Status is not recognised."
        End If
        Return Nothing
    End Function
End Module
