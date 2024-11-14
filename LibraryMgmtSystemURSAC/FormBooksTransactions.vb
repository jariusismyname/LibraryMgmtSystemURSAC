﻿Imports AForge.Video
Imports AForge.Video.DirectShow
Imports ZXing
Imports System.Data.SqlClient

Public Class FormBooksTransactions
    Private camera As VideoCaptureDevice
    Private bmp As Bitmap
    Private connectionString As String = "Data Source=DESKTOP-D5V36F0\SQLEXPRESS;Initial Catalog=LibraryManagementSystem;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"

    Private Sub btnScan_Click(sender As Object, e As EventArgs) Handles btnScan.Click
        ' Start camera
        Dim videoDevices As New FilterInfoCollection(FilterCategory.VideoInputDevice)
        camera = New VideoCaptureDevice(videoDevices(0).MonikerString)
        AddHandler camera.NewFrame, New NewFrameEventHandler(AddressOf CaptureFrame)
        camera.Start()
    End Sub

    Private Sub CaptureFrame(sender As Object, eventArgs As NewFrameEventArgs)
        ' Clone the frame to avoid the bitmap being locked by multiple processes
        If bmp IsNot Nothing Then
            bmp.Dispose() ' Dispose of the previous bitmap to avoid memory leaks
        End If

        bmp = DirectCast(eventArgs.Frame.Clone(), Bitmap)
        PictureBoxCamera.Image = DirectCast(bmp.Clone(), Bitmap) ' Display the cloned bitmap

        ' Barcode reader
        Dim reader As New BarcodeReader()
        Dim result As Result = reader.Decode(bmp)

        If result IsNot Nothing Then
            txtISBN.Invoke(Sub() txtISBN.Text = result.Text) ' Display scanned ISBN
            camera.SignalToStop() ' Stop the camera
            UpdateDatabase(result.Text) ' Call the update function
        End If
    End Sub


    Private Sub UpdateDatabase(isbn As String)
        ' Update available and borrowed books in the database
        Using connection As New SqlConnection(connectionString)
            connection.Open()

            Dim query As String = "UPDATE Books SET AvailableBooks = AvailableBooks - 1, BorrowedBooks = BorrowedBooks + 1 WHERE ISBN = @ISBN AND AvailableBooks > 0"
            Using command As New SqlCommand(query, connection)
                command.Parameters.AddWithValue("@ISBN", isbn)

                Dim rowsAffected As Integer = command.ExecuteNonQuery()

                If rowsAffected > 0 Then
                    MessageBox.Show("Book borrowed successfully!")
                    'Using connections As New SqlConnection(connectionString)
                    '    connections.Open()

                    '    Dim querys As String = "UPDATE LibMgmtSystem SET AvailableBooks = AvailableBooks - 1, BorrowedBooks = BorrowedBooks + 1 WHERE ISBN = @ISBN AND AvailableBooks > 0"
                    '    Using commands As New SqlCommand(querys, connections)
                    '        commands.Parameters.AddWithValue("@ISBN", isbn)

                    '    End Using
                    'End Using ' Check if PNLRETURNDATE needs to be invoked
                    If PNLRETURNDATE.InvokeRequired Then
                        PNLRETURNDATE.Invoke(Sub()
                                                 PNLRETURNDATE.Visible = True
                                             End Sub)
                    Else
                        PNLRETURNDATE.Visible = True
                    End If

                    ' Check if PNLISBN needs to be invoked
                    If PNLISBN.InvokeRequired Then
                        PNLISBN.Invoke(Sub()
                                           PNLISBN.Visible = False
                                       End Sub)
                    Else
                        PNLISBN.Visible = False
                    End If
                    InsertISBNToTransaction(txtISBN.Text)


                Else
                    MessageBox.Show("No available copies of this book.")
                End If
            End Using
        End Using
    End Sub
    Private Sub InsertISBNToTransaction(isbn As String)
        ' Define the connection string
        Dim connString As String = "Data Source=DESKTOP-D5V36F0\SQLEXPRESS;Initial Catalog=LibraryManagementSystem;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"
        Dim conn As New SqlConnection(connString)

        Try
            ' Open the connection
            conn.Open()

            ' Define the SQL Insert command
            Dim sqlInsert As String = "INSERT INTO Transactions (ISBN ) VALUES (@ISBN )"

            ' Create the SQL command
            Dim cmd As New SqlCommand(sqlInsert, conn)

            ' Add parameters to prevent SQL injection
            cmd.Parameters.AddWithValue("@ISBN", isbn)

            ' Execute the command
            Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

            ' Check if the insertion was successful
            If rowsAffected > 0 Then
                'MessageBox.Show("Book borrowed successfully!")
            Else
                MessageBox.Show("Borrow operation failed.")
            End If

        Catch ex As Exception
            ' Handle any exceptions
            MessageBox.Show("Error occurred: " & ex.Message)
        Finally
            ' Ensure the connection is closed
            conn.Close()
        End Try
    End Sub
    'Private Sub txtISBN_LostFocus(sender As Object, e As EventArgs) Handles txtISBN.LostFocus
    '    ' Automatically insert ISBN when the user finishes entering it
    '    InsertISBNToTransaction(txtISBN.Text)
    'End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If camera IsNot Nothing AndAlso camera.IsRunning Then
            camera.SignalToStop() ' Stop the camera when form closes
        End If
    End Sub

    'Private Sub Button1_Click(sender As Object, e As EventArgs)
    '    PNLLOGIN.Visible = False
    '    PNLISBN.Visible = True
    'End Sub
    Dim todayDate As Date = Today  ' Get today's date

    'Private Sub UpdateAddedDate()  ' Renamed for clarity
    '    Using connection As New SqlConnection(connectionString)
    '        connection.Open()

    '        Dim query As String = "UPDATE Students SET AddedDate = @AddedDate WHERE AddedDate IS NULL"
    '        Using command As New SqlCommand(query, connection)
    '            command.Parameters.AddWithValue("@AddedDate", todayDate)  ' Use todayDate variable
    '            command.ExecuteNonQuery()
    '        End Using
    '    End Using
    'End Sub
    Private Sub UpdateDate(librarySystem As String)
        Using connection As New SqlConnection(connectionString)
            connection.Open()

            Dim query As String = "UPDATE Transactions SET ReturnDate = @ReturnDate WHERE ReturnDate IS NULL"
            Using command As New SqlCommand(query, connection)
                command.Parameters.AddWithValue("@ReturnDate", librarySystem)
                command.ExecuteNonQuery()
            End Using
        End Using
    End Sub
    Private Sub DateReturn()
        ' Check if the selected date is a past date compared to today's date
        If DateTimePicker1.Value.Date < DateTime.Now.Date Then
            ' Show an error message if the selected date is in the past
            MessageBox.Show("Error: The selected date cannot be in the past. Please choose a valid return date.", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Error)

            ' Optionally, reset the DateTimePicker to today's date
            DateTimePicker1.Value = DateTime.Now.Date
        Else
            UpdateDate(DateTimePicker1.Value.Date)
            'Dim todayDate As Date = Today  ' Today contains the current date
            'UpdateAddedDate()

            PNLSTUDENTNUMBER.Visible = True
            PNLRETURNDATE.Visible = False
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        DateReturn()

    End Sub
    Private Sub DataGridView1_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridViewBooks.CellClick
        ' Make sure the user selects a valid row (not a header or an empty row)
        If e.RowIndex >= 0 Then
            ' Get the selected row
            Dim selectedRow As DataGridViewRow = DataGridViewBooks.Rows(e.RowIndex)

            ' Fill the textboxes with the data from the selected row
            txtISBN_.Text = selectedRow.Cells("ISBN").Value.ToString()  ' Assuming the column name is 'ISBN'
            txtBookname.Text = selectedRow.Cells("BookName").Value.ToString()  ' Assuming the column name is 'BookName'
            txtAvailableBooks.Text = selectedRow.Cells("AvailableBooks").Value.ToString()  ' Assuming the column name is 'AvailableBooks'
        End If
    End Sub
    Private Sub btnadd2_Click(sender As Object, e As EventArgs) Handles btnadd2.Click
        If String.IsNullOrWhiteSpace(txtstudentName_.Text) OrElse String.IsNullOrWhiteSpace(txtstudentnumber.Text) OrElse String.IsNullOrWhiteSpace(txtcourse.Text) Then
            MessageBox.Show("Please fill in all required fields.")
            Exit Sub ' Exit the procedure to prevent the insert operation
        End If

        ' Check if the StudentNumber already exists
        Dim checkSql As String = "SELECT COUNT(*) FROM Students WHERE StudentNumber = @StudentNumber"
        Dim studentExists As Integer

        Using conn As New SqlConnection(connectionString)
            Dim checkCmd As New SqlCommand(checkSql, conn)
            checkCmd.Parameters.AddWithValue("@StudentNumber", txtstudentnumber.Text)

            Try
                conn.Open()
                studentExists = Convert.ToInt32(checkCmd.ExecuteScalar())
                conn.Close()

                ' If StudentNumber exists, stop the operation
                If studentExists > 0 Then
                    MessageBox.Show("A student with this Student Number already exists. Please use a unique Student Number.")
                    Exit Sub
                End If

            Catch ex As Exception
                MessageBox.Show("Error: " & ex.Message)
                Exit Sub
            End Try
        End Using

        ' Proceed with the insert if no duplicate is found
        Dim sqlInsert As String = "INSERT INTO Students (StudentName, StudentNumber, Course, DateAdded) VALUES (@StudentName, @StudentNumber, @Course, @DateAdded)"

        Using conn As New SqlConnection(connectionString)
            Dim cmd As New SqlCommand(sqlInsert, conn)

            ' Add parameters for StudentName, StudentNumber, Course, and DateAdded
            cmd.Parameters.AddWithValue("@StudentName", txtstudentName_.Text)
            cmd.Parameters.AddWithValue("@StudentNumber", txtstudentnumber.Text)
            cmd.Parameters.AddWithValue("@Course", txtcourse.Text)

            ' Add the current date for the DateAdded column
            cmd.Parameters.AddWithValue("@DateAdded", DateTime.Now)

            Try
                conn.Open()
                Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                ' Check if the insert operation was successful
                If rowsAffected > 0 Then
                    MessageBox.Show("Student added successfully!")
                    LoadDataIntoDataGridView2() ' Refresh the DataGridView
                    CLEAR()

                Else
                    MessageBox.Show("Add operation failed.")
                End If
            Catch ex As Exception
                MessageBox.Show("Error: " & ex.Message)
            End Try
        End Using


    End Sub

    Private Sub btndelete2_Click(sender As Object, e As EventArgs) Handles btndelete2.Click
        ' Check if a row is selected
        If DataGridViewStudents.SelectedRows.Count > 0 Then
            ' Get the selected row
            Dim selectedRow As DataGridViewRow = DataGridViewStudents.SelectedRows(0)
            Dim studentNumberId As String = selectedRow.Cells("StudentNumberId").Value.ToString() ' Replace with the actual primary key column name

            ' Define the SQL Delete command
            Dim sqlDelete As String = "DELETE FROM Students WHERE StudentNumberId = @StudentNumberId"

            Using conn As New SqlConnection(connectionString)
                Dim cmd As New SqlCommand(sqlDelete, conn)
                cmd.Parameters.AddWithValue("@StudentNumberId", studentNumberId)

                Try
                    conn.Open()
                    Dim rowsAffected As Integer = cmd.ExecuteNonQuery()
                    If rowsAffected > 0 Then
                        MessageBox.Show("Student deleted successfully!")
                        LoadDataIntoDataGridView2()
                        CLEAR()

                    Else
                        MessageBox.Show("Delete operation failed.")
                    End If
                Catch ex As Exception
                    MessageBox.Show("Error: " & ex.Message)
                End Try
            End Using
        Else
            MessageBox.Show("Please select a row to delete.")
        End If
    End Sub

    Private Sub DataGridView2_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridViewStudents.CellClick
        ' Make sure the user selects a valid row (not a header or an empty row)
        If e.RowIndex >= 0 Then
            ' Get the selected row
            Dim selectedRow As DataGridViewRow = DataGridViewStudents.Rows(e.RowIndex)

            ' Fill the textboxes with the data from the selected row
            txtstudentnumber.Text = selectedRow.Cells("StudentNumber").Value.ToString()  ' Assuming the column name is 'StudentNumber'
            txtstudentName_.Text = selectedRow.Cells("StudentName").Value.ToString()  ' Assuming the column name is 'StudentName'
            txtcourse.Text = selectedRow.Cells("Course").Value.ToString()  ' Assuming the column name is 'Course'
        End If
    End Sub

    'Private Sub FormBooksTransactions(sender As Object, e As EventArgs) Handles MyBase.Load
    '    LoadDataIntoDataGridView()
    '    LoadDataIntoDataGridView2()
    '    ' Example of code that might override designer settings:
    '    'Me.BackColor = Color.White  ' This would override any background color set in the designer.

    'End Sub
    'Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    '    LoadDataIntoDataGridView()
    '    LoadDataIntoDataGridView2()
    '    ' Example of code that might override designer settings:
    '    'Me.BackColor = Color.White  ' This would override any background color set in the designer.

    'End Sub
    Private Sub LoadDataIntoDataGridView()
        ' Define the connection object
        Dim conn As New SqlConnection(connectionString)

        Try
            ' Open the connection
            conn.Open()

            ' SQL query to retrieve data from your table (e.g., Transactions)
            Dim sqlQuery As String = "SELECT * FROM Books"

            ' Create a DataAdapter to fetch data
            Dim dataAdapter As New SqlDataAdapter(sqlQuery, conn)

            ' Create a DataTable to hold the fetched data
            Dim dataTable As New DataTable()

            ' Fill the DataTable with the results from the SQL query
            dataAdapter.Fill(dataTable)

            ' Bind the DataTable to the DataGridView
            DataGridViewBooks.DataSource = dataTable

        Catch ex As Exception
            ' Display any errors that occur
            MessageBox.Show("Error: " & ex.Message)
        Finally
            ' Ensure the connection is closed
            conn.Close()
        End Try
    End Sub
    Private Sub LoadDataInDataGridViewTRANSACTIONS()
        ' Define the connection object
        Dim conn As New SqlConnection(connectionString)

        Try
            ' Open the connection
            conn.Open()

            ' SQL query to retrieve data from your table (e.g., Transactions)
            Dim sqlQuery As String = "SELECT * FROM Transactions"

            ' Create a DataAdapter to fetch data
            Dim dataAdapter As New SqlDataAdapter(sqlQuery, conn)

            ' Create a DataTable to hold the fetched data
            Dim dataTable As New DataTable()

            ' Fill the DataTable with the results from the SQL query
            dataAdapter.Fill(dataTable)

            ' Bind the DataTable to the DataGridView
            DataGridViewTransactions.DataSource = dataTable

        Catch ex As Exception
            ' Display any errors that occur
            MessageBox.Show("Error: " & ex.Message)
        Finally
            ' Ensure the connection is closed
            conn.Close()
        End Try
    End Sub
    Private Sub btnupdate2_Click(sender As Object, e As EventArgs) Handles btnupdate2.Click
        ' Check if a row is selected
        If DataGridViewStudents.SelectedRows.Count > 0 Then
            ' Get the selected row
            Dim selectedRow As DataGridViewRow = DataGridViewStudents.SelectedRows(0)
            Dim studentNumberId As String = selectedRow.Cells("StudentNumberId").Value.ToString() ' Replace with the actual primary key column name

            ' Define the SQL query to check for duplicate StudentNumber
            Dim sqlCheckDuplicate As String = "SELECT COUNT(*) FROM Students WHERE StudentNumber = @StudentNumber AND StudentNumberId <> @StudentNumberId"

            Using conn As New SqlConnection(connectionString)
                Dim cmdCheck As New SqlCommand(sqlCheckDuplicate, conn)
                cmdCheck.Parameters.AddWithValue("@StudentNumber", txtstudentnumber.Text)
                cmdCheck.Parameters.AddWithValue("@StudentNumberId", studentNumberId)

                Try
                    conn.Open()

                    ' Execute the query to check for duplicates
                    Dim duplicateCount As Integer = Convert.ToInt32(cmdCheck.ExecuteScalar())

                    If duplicateCount > 0 Then
                        MessageBox.Show("StudentNumber already exists for another student. Update cannot proceed.")
                    Else
                        ' Define the SQL Update command
                        Dim sqlUpdate As String = "UPDATE Students SET StudentName = @StudentName, StudentNumber = @StudentNumber, Course = @Course WHERE StudentNumberId = @StudentNumberId"

                        Dim cmdUpdate As New SqlCommand(sqlUpdate, conn)
                        cmdUpdate.Parameters.AddWithValue("@StudentName", txtstudentName_.Text)
                        cmdUpdate.Parameters.AddWithValue("@StudentNumber", txtstudentnumber.Text)
                        cmdUpdate.Parameters.AddWithValue("@Course", txtcourse.Text)
                        cmdUpdate.Parameters.AddWithValue("@StudentNumberId", studentNumberId)

                        ' Execute the update
                        Dim rowsAffected As Integer = cmdUpdate.ExecuteNonQuery()
                        If rowsAffected > 0 Then
                            MessageBox.Show("Student updated successfully!")
                            LoadDataIntoDataGridView2()
                            CLEAR()
                        Else
                            MessageBox.Show("Update failed.")
                        End If
                    End If
                Catch ex As Exception
                    MessageBox.Show("Error: " & ex.Message)
                End Try
            End Using
        Else
            MessageBox.Show("Please select a row to update.")
        End If

    End Sub

    Private Sub LoadDataIntoDataGridView2()
        ' Define the connection object
        Dim conn As New SqlConnection(connectionString)

        Try
            ' Open the connection
            conn.Open()

            ' SQL query to retrieve data from your table (e.g., Transactions)
            Dim sqlQuery As String = "SELECT * FROM Students"

            ' Create a DataAdapter to fetch data
            Dim dataAdapter As New SqlDataAdapter(sqlQuery, conn)

            ' Create a DataTable to hold the fetched data
            Dim dataTable As New DataTable()

            ' Fill the DataTable with the results from the SQL query
            dataAdapter.Fill(dataTable)

            ' Bind the DataTable to the DataGridView
            DataGridViewStudents.DataSource = dataTable

        Catch ex As Exception
            ' Display any errors that occur
            MessageBox.Show("Error: " & ex.Message)
        Finally
            ' Ensure the connection is closed
            conn.Close()
        End Try
    End Sub
    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If DataGridViewBooks.SelectedRows.Count = 0 Then
            MessageBox.Show("Please select a row to delete.")
            Return
        End If

        ' Get the selected row
        Dim selectedRow As DataGridViewRow = DataGridViewBooks.SelectedRows(0)

        ' Extract the BookId from the selected row
        Dim bookId As Integer = Convert.ToInt32(selectedRow.Cells("BookId").Value)

        ' Create the SQL delete command
        Dim sqlDelete As String = "DELETE FROM Books WHERE BookId = @BookId"

        Using conn As New SqlConnection(connectionString)
            Try
                conn.Open()
                Dim cmd As New SqlCommand(sqlDelete, conn)
                cmd.Parameters.AddWithValue("@BookId", bookId)

                Dim rowsAffected As Integer = cmd.ExecuteNonQuery()
                If rowsAffected > 0 Then
                    MessageBox.Show("Book deleted successfully!")
                    LoadDataIntoDataGridView() ' Refresh the DataGridView
                    CLEAR()

                Else
                    MessageBox.Show("Delete operation failed.")
                End If
            Catch ex As Exception
                MessageBox.Show("Error occurred: " & ex.Message)
            End Try
        End Using
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        ' Check if any of the text boxes are empty
        If String.IsNullOrWhiteSpace(txtBookname.Text) OrElse
           String.IsNullOrWhiteSpace(txtISBN_.Text) OrElse
           String.IsNullOrWhiteSpace(txtAvailableBooks.Text) Then
            MessageBox.Show("Please fill in all the fields.")
            Return
        End If

        ' Create the SQL query to check if the ISBN already exists
        Dim sqlCheckISBN As String = "SELECT COUNT(*) FROM Books WHERE ISBN = @ISBN"

        ' Create the SQL insert command
        Dim sqlInsert As String = "INSERT INTO Books (BookName, ISBN, AvailableBooks, BorrowedBooks) VALUES (@BookName, @ISBN, @AvailableBooks, @BorrowedBooks)"

        Using conn As New SqlConnection(connectionString)
            Try
                conn.Open()

                ' Check if the ISBN already exists
                Dim checkCmd As New SqlCommand(sqlCheckISBN, conn)
                checkCmd.Parameters.AddWithValue("@ISBN", txtISBN_.Text)

                Dim isbnCount As Integer = CInt(checkCmd.ExecuteScalar())

                If isbnCount > 0 Then
                    MessageBox.Show("A book with this ISBN already exists.")
                    Return ' Exit the function if ISBN already exists
                End If

                ' Proceed with the insert if the ISBN does not exist
                Dim insertCmd As New SqlCommand(sqlInsert, conn)
                insertCmd.Parameters.AddWithValue("@BookName", txtBookname.Text)
                insertCmd.Parameters.AddWithValue("@ISBN", txtISBN_.Text)
                insertCmd.Parameters.AddWithValue("@AvailableBooks", txtAvailableBooks.Text)
                insertCmd.Parameters.AddWithValue("@BorrowedBooks", 0) ' Set BorrowedBooks to 0 for new entries

                Dim rowsAffected As Integer = insertCmd.ExecuteNonQuery()
                If rowsAffected > 0 Then
                    MessageBox.Show("Book added successfully!")
                    LoadDataIntoDataGridView() ' Refresh the DataGridView
                    CLEAR()
                Else
                    MessageBox.Show("Add operation failed.")
                End If

            Catch ex As Exception
                MessageBox.Show("Error occurred: " & ex.Message)
            End Try
        End Using

    End Sub
    'Private Sub UpdateTransactions()
    '    Dim studentNumber As String = txtstudentnumber_.Text

    '    ' Check if txtstudentnumber_ is not empty
    '    If String.IsNullOrEmpty(studentNumber) Then
    '        MessageBox.Show("Student Number is empty.")
    '        Return
    '    End If

    '    Using connection As New SqlConnection(connectionString)
    '        connection.Open()

    '        ' Check if the StudentNumber exists in the Students table
    '        Dim checkQuery As String = "SELECT COUNT(*) FROM Students WHERE StudentNumber = @StudentNumber"
    '        Using checkCommand As New SqlCommand(checkQuery, connection)
    '            checkCommand.Parameters.AddWithValue("@StudentNumber", studentNumber)
    '            Dim studentExists As Integer = Convert.ToInt32(checkCommand.ExecuteScalar())

    '            If studentExists = 0 Then
    '                MessageBox.Show("Student Number not found in the Students table.")
    '                Return
    '            End If
    '        End Using

    '        ' Update the Transactions table for the most recent entry with NULL TransactionId
    '        Dim updateQuery As String = "UPDATE Transactions SET StudentNumber = @StudentNumber WHERE TransactionId IS NULL AND TransactionId = (SELECT MAX(TransactionId) FROM Transactions WHERE TransactionId IS NULL)"
    '        Using updateCommand As New SqlCommand(updateQuery, connection)
    '            updateCommand.Parameters.AddWithValue("@StudentNumber", studentNumber)
    '            Dim rowsAffected As Integer = updateCommand.ExecuteNonQuery()

    '            If rowsAffected > 0 Then
    '                MessageBox.Show("Transaction updated successfully!")
    '            Else
    '                MessageBox.Show("No transactions with NULL TransactionId were found.")
    '            End If
    '        End Using
    '    End Using
    'End Sub

    Private Sub btnUpdate_Click(sender As Object, e As EventArgs) Handles btnUpdate.Click
        If DataGridViewBooks.SelectedRows.Count = 0 Then
            MessageBox.Show("Please select a row to update.")
            Return
        End If

        ' Get the selected row
        Dim selectedRow As DataGridViewRow = DataGridViewBooks.SelectedRows(0)

        ' Extract the BookId from the selected row
        Dim bookId As Integer = Convert.ToInt32(selectedRow.Cells("BookId").Value)

        ' Create the SQL query to check for duplicate ISBNs
        Dim sqlCheckISBN As String = "SELECT COUNT(*) FROM Books WHERE ISBN = @ISBN AND BookId <> @BookId"

        Using conn As New SqlConnection(connectionString)
            Try
                conn.Open()

                ' Check for duplicate ISBNs
                Dim checkCmd As New SqlCommand(sqlCheckISBN, conn)
                checkCmd.Parameters.AddWithValue("@ISBN", txtISBN_.Text)
                checkCmd.Parameters.AddWithValue("@BookId", bookId)

                Dim isbnCount As Integer = Convert.ToInt32(checkCmd.ExecuteScalar())
                If isbnCount > 0 Then
                    MessageBox.Show("Another book with the same ISBN already exists. Update aborted.")
                    Return
                End If

                ' Create the SQL update command
                Dim sqlUpdate As String = "UPDATE Books SET BookName = @BookName, ISBN = @ISBN, AvailableBooks = @AvailableBooks WHERE BookId = @BookId"

                ' Update the book record
                Dim cmd As New SqlCommand(sqlUpdate, conn)
                cmd.Parameters.AddWithValue("@BookName", txtBookname.Text)
                cmd.Parameters.AddWithValue("@ISBN", txtISBN_.Text)
                cmd.Parameters.AddWithValue("@AvailableBooks", txtAvailableBooks.Text)
                cmd.Parameters.AddWithValue("@BookId", bookId)

                Dim rowsAffected As Integer = cmd.ExecuteNonQuery()
                If rowsAffected > 0 Then
                    MessageBox.Show("Book updated successfully!")
                    LoadDataIntoDataGridView() ' Refresh the DataGridView
                    CLEAR()
                Else
                    MessageBox.Show("Update failed.")
                End If
            Catch ex As Exception
                MessageBox.Show("Error occurred: " & ex.Message)
            End Try
        End Using

    End Sub

    Private Sub BTNLOGIN_Click(sender As Object, e As EventArgs) Handles BTNLOGIN.Click

        'USER
        If TXTPASSWORD.Text = "PASSWORD" And TXTUSERNAME.Text = "USER" Then
            PNLLOGIN.Visible = False
            PNLISBN.Visible = True
            MessageBox.Show("LOGIN SUCCESSFULL")
            TXTPASSWORD.Text = ""
            TXTPASSWORD.Text = ""
            TXTUSERNAME.Text = ""
            btndelete2.Visible = False
            btnupdate2.Visible = False
            DeleteStudentsAfterFourYears()
            'ADMIN
        ElseIf TXTPASSWORD.Text = "PASSWORD" And TXTUSERNAME.Text = "ADMIN" Then
            PNLBOOKS.Visible = True
            PNLLOGIN.Visible = False
            MessageBox.Show("LOGIN SUCCESSFULL")
            TXTPASSWORD.Text = ""
            TXTUSERNAME.Text = ""
            LoadDataIntoDataGridView()
            DeleteStudentsAfterFourYears()

        Else
            MessageBox.Show("WRONG INPUT")
        End If
    End Sub
    Private Sub UpdateStudentNumber(StudentNumber As String)
        Dim exists As Boolean = False
        Dim count As Integer = 0

        Using connection As New SqlConnection(connectionString)
            connection.Open()

            ' Check if student number already exists
            Dim checkQuery As String = "SELECT COUNT(*) FROM Students WHERE StudentNumber = @StudentNumber"
            Using checkCommand As New SqlCommand(checkQuery, connection)
                checkCommand.Parameters.AddWithValue("@StudentNumber", StudentNumber)
                exists = CInt(checkCommand.ExecuteScalar()) > 0
            End Using

            ' Count existing student numbers in Transactions
            Dim countQuery As String = "SELECT COUNT(*) FROM Transactions WHERE StudentNumber IS NOT NULL"
            Using countCommand As New SqlCommand(countQuery, connection)
                count = CInt(countCommand.ExecuteScalar())
            End Using

            If exists Then
                If count < 3 Then  ' Only update if less than 3 student numbers exist
                    ' Update Transactions table
                    Dim updateQuery As String = "UPDATE Transactions SET StudentNumber = @StudentNumber WHERE StudentNumber IS NULL"
                    Using updateCommand As New SqlCommand(updateQuery, connection)
                        updateCommand.Parameters.AddWithValue("@StudentNumber", StudentNumber)
                        updateCommand.ExecuteNonQuery()
                    End Using
                    PNLAGAIN.Visible = True
                    PNLSTUDENTNUMBER.Visible = False
                Else
                    MsgBox("Maximum of 3 student numbers allowed in Transactions.", MsgBoxStyle.Information)
                End If
            Else
                MsgBox("Student number " & StudentNumber & " doesn't exist.", MsgBoxStyle.Information)
            End If
        End Using
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim studentNumber As String = txtstudentnumber_.Text

        If Not String.IsNullOrEmpty(studentNumber) Then
            UpdateStudentNumber(studentNumber)
        Else
            MsgBox("Please enter a student number.", MsgBoxStyle.Information)
        End If
        Main()

    End Sub

    Sub Main()

        Using connection As New SqlConnection(connectionString)
            connection.Open()

            Dim command As New SqlCommand("UPDATE Transactions SET Status = 'Borrowed' WHERE Status IS NULL", connection)

            Dim rowsAffected As Integer = command.ExecuteNonQuery()
            Console.WriteLine("{0} rows updated.", rowsAffected)
        End Using

        Console.ReadLine()
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click

        ' Display a confirmation message box
        Dim result As DialogResult = MessageBox.Show("Do you want to proceed?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        ' Check the user's response
        If result = DialogResult.Yes Then
            PNLLOGIN.Visible = True ' Make PNLLOGIN panel visible
            PNLISBN.Visible = False ' Make PNLLOGIN panel visible

        Else
            ' Do nothing if the user selects No or closes the message box
        End If
    End Sub
    Private Sub LOGOUT()

        ' Display a confirmation message box
        Dim result As DialogResult = MessageBox.Show("Do you want to proceed?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        ' Check the user's response
        If result = DialogResult.Yes Then
            PNLLOGIN.Visible = True ' Make PNLLOGIN panel visible
            PNLSTUDENTNUMBER.Visible = False ' Make PNLLOGIN panel visible

        Else
            ' Do nothing if the user selects No or closes the message box
        End If
    End Sub
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        ' Display a confirmation message box
        Dim result As DialogResult = MessageBox.Show("Do you want to proceed?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        ' Check the user's response
        If result = DialogResult.Yes Then
            PNLLOGIN.Visible = True ' Make PNLLOGIN panel visible
            PNLAGAIN.Visible = False ' Make PNLLOGIN panel visible

        Else
            ' Do nothing if the user selects No or closes the message box
        End If
    End Sub
    Private Sub DeleteStudentsAfterFourYears()
        Using connection As New SqlConnection(connectionString)
            connection.Open()

            Dim deleteQuery As String = "DELETE FROM Students WHERE DATEDIFF(YEAR, DateAdded, GETDATE()) >= 4"
            Using command As New SqlCommand(deleteQuery, connection)
                command.ExecuteNonQuery()
            End Using
        End Using
    End Sub
    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click

        '' Display a confirmation message box
        'Dim result As DialogResult = MessageBox.Show("Do you want to proceed?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        '' Check the user's response
        'If result = DialogResult.Yes Then
        PNLISBN.Visible = True ' Make PNLLOGIN panel visible
        PNLSTUDENTS.Visible = False ' Make PNLLOGIN panel visible

        'Else
        '    ' Do nothing if the user selects No or closes the message box
        'End If
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        ' Display a confirmation message box
        Dim result As DialogResult = MessageBox.Show("Do you want to proceed?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        ' Check the user's response
        If result = DialogResult.Yes Then
            PNLLOGIN.Visible = True ' Make PNLLOGIN panel visible
            PNLBOOKS.Visible = False ' Make PNLLOGIN panel visible

        Else
            ' Do nothing if the user selects No or closes the message box
        End If
    End Sub

    'Private Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click
    '    LOGOUT()
    'End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        DeleteStudentsAfterFourYears()
        PNLISBN.Visible = False
        PNLSTUDENTS.Visible = True
        Button6.Visible = False
        Button5.Visible = True
        LoadDataIntoDataGridView2()
        DeleteStudentsAfterFourYears()
    End Sub

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click
        PNLRETURNBOOK.Visible = True
        PNLISBN.Visible = False

    End Sub

    Private Sub Button11_Click(sender As Object, e As EventArgs) Handles Button11.Click
        ' Define the SQL DELETE command - ensure only one row is deleted based on TransactionId
        Dim sqlDelete As String = "DELETE TOP (1) FROM Transactions WHERE StudentNumber = @StudentNumber AND ISBN = @ISBN"

        ' Check if textboxes have values
        If String.IsNullOrEmpty(TXTSTUDENTNUMBER___.Text) OrElse String.IsNullOrEmpty(TXTISBN___.Text) Then
            MessageBox.Show("Please provide both Student Number and ISBN to delete a transaction.")
            Exit Sub
        End If

        ' Create a new SqlConnection using the connection string
        Using conn As New SqlConnection(connectionString)
            Dim cmd As New SqlCommand(sqlDelete, conn)

            ' Add parameters for the StudentNumber and ISBN
            cmd.Parameters.AddWithValue("@StudentNumber", TXTSTUDENTNUMBER___.Text)
            cmd.Parameters.AddWithValue("@ISBN", TXTISBN___.Text)

            Try
                ' Open the connection
                conn.Open()

                ' Execute the DELETE command
                Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                ' Check if the delete operation was successful
                If rowsAffected > 0 Then
                    MessageBox.Show("Transaction deleted successfully!")
                    LoadDataIntoDataGridView2() ' Refresh the DataGridView if necessary

                Else
                    MessageBox.Show("No matching transaction found.")
                End If

            Catch ex As Exception
                ' Handle any exceptions that occur during the operation
                MessageBox.Show("Error: " & ex.Message)
            Finally
                ' Close the connection if it's open
                If conn.State = ConnectionState.Open Then
                    conn.Close()
                End If
            End Try
        End Using

    End Sub


    Private Sub Button12_Click(sender As Object, e As EventArgs) Handles Button12.Click
        'If result = DialogResult.Yes Then
        PNLISBN.Visible = True ' Make PNLLOGIN panel visible
        PNLRETURNBOOK.Visible = False ' Make PNLLOGIN panel visible

    End Sub
    Private Sub CLEAR()
        TXTUSERNAME.Text = ""
        txtAvailableBooks.Text = ""
        txtBookname.Text = ""
        txtcourse.Text = ""
        txtISBN.Text = ""
        txtISBN_.Text = ""
        txtstudentName_.Text = ""
        txtstudentnumber.Text = ""
        txtstudentnumber_.Text = ""

    End Sub
    Private Sub Button13_Click(sender As Object, e As EventArgs) Handles Button13.Click
        PNLBOOKS.Visible = False
        PNLSTUDENTS.Visible = True
        btnupdate2.Visible = True
        btndelete2.Visible = True
        Button5.Visible = False
        Button6.Visible = True
        LoadDataIntoDataGridView2()
        DeleteStudentsAfterFourYears()
    End Sub
    Private Sub txtNumbersAndSpaces_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtISBN.KeyPress, txtAvailableBooks.KeyPress, txtISBN_.KeyPress, TXTISBN___.KeyPress
        ' Allow only numbers, spaces, and control keys (like backspace)
        If Not Char.IsDigit(e.KeyChar) AndAlso Not Char.IsControl(e.KeyChar) AndAlso Not e.KeyChar = " "c Then
            e.Handled = True
        End If
    End Sub
    Private Sub txtLettersAndSpaces_KeyPress(sender As Object, e As KeyPressEventArgs) Handles txtBookname.KeyPress, txtcourse.KeyPress, txtstudentName_.KeyPress, TXTUSERNAME.KeyPress
        ' Allow only letters, spaces, and control keys (like backspace)
        If Not Char.IsLetter(e.KeyChar) AndAlso Not Char.IsControl(e.KeyChar) AndAlso Not e.KeyChar = " "c Then
            e.Handled = True
        End If
    End Sub


    Private Sub Button22_Click(sender As Object, e As EventArgs) Handles Button22.Click
        CLEAR()
        PNLAGAIN.Visible = False
        PNLISBN.Visible = True
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        PNLBOOKS.Visible = True ' Make PNLLOGIN panel visible
        PNLSTUDENTS.Visible = False ' Make PNLLOGIN panel visible

    End Sub

    Private Sub Button14_Click(sender As Object, e As EventArgs) Handles Button14.Click
        PNLTRANSACTIONS.VISIBLE = True
        PNLBOOKS.Visible = False
        LoadDataInDataGridViewTRANSACTIONS()
        Button6.Visible = True

    End Sub

    Private Sub Button15_Click(sender As Object, e As EventArgs) Handles Button15.Click
        PNLBOOKS.Visible = True ' Make PNLLOGIN panel visible
        PNLTRANSACTIONS.Visible = False ' Make PNLLOGIN panel visible

    End Sub
    Private Sub FineAmount()
        Dim sqlUpdate As String = "UPDATE Transactions SET FineAmount = CASE " &
                              "WHEN DATEDIFF(DAY, ReturnDate, @Today) * 50 < 0 THEN 0.00 " &
                              "ELSE DATEDIFF(DAY, ReturnDate, @Today) * 50 END"

        ' Use your actual connection string here
        Using conn As New SqlConnection(connectionString)
            Dim cmd As New SqlCommand(sqlUpdate, conn)

            ' Add the current date as a parameter
            cmd.Parameters.AddWithValue("@Today", DateTime.Now)

            Try
                conn.Open()
                Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                ' Check if the update operation was successful
                If rowsAffected > 0 Then
                    MessageBox.Show("Fine amounts updated successfully for all rows.")
                Else
                    MessageBox.Show("No rows were updated.")
                End If
            Catch ex As Exception
                MessageBox.Show("Error: " & ex.Message)
            End Try
        End Using
    End Sub

    Private Sub Button16_Click(sender As Object, e As EventArgs) Handles Button16.Click
        FineAmount()
        LoadDataInDataGridViewTRANSACTIONS()

    End Sub
    Private Sub btnSearchStudents_Click(sender As Object, e As EventArgs) Handles btnsearchstudents.Click
        Dim searchQuery As String = "SELECT * FROM Students WHERE StudentName LIKE @Search OR StudentNumber LIKE @Search OR Course LIKE @Search"

        Using conn As New SqlConnection(connectionString)
            Dim cmd As New SqlCommand(searchQuery, conn)
            cmd.Parameters.AddWithValue("@Search", "%" & txtsearchstudents.Text & "%")

            Try
                conn.Open()
                Dim adapter As New SqlDataAdapter(cmd)
                Dim table As New DataTable()
                adapter.Fill(table)

                ' Display the search results in DataGridView
                DataGridViewStudents.DataSource = table
            Catch ex As Exception
                MessageBox.Show("Error: " & ex.Message)
            End Try
        End Using
    End Sub

    Private Sub btnsearchtransactions_Click(sender As Object, e As EventArgs) Handles btnsearchtransactions.Click
        ' SQL query to search Transactions based on input in txtsearchtransactions
        Dim sqlSearch As String = "SELECT * FROM Transactions WHERE ISBN LIKE @search OR Status LIKE @search OR StudentNumber LIKE @search"

        Using conn As New SqlConnection(connectionString)
            Dim cmd As New SqlCommand(sqlSearch, conn)
            ' Parameterize the query to avoid SQL injection
            cmd.Parameters.AddWithValue("@search", "%" & txtsearchtransactions.Text & "%")

            Dim adapter As New SqlDataAdapter(cmd)
            Dim dt As New DataTable()

            Try
                conn.Open()
                adapter.Fill(dt)
                ' Bind the results to the DataGridView
                DataGridViewTransactions.DataSource = dt
            Catch ex As Exception
                MessageBox.Show("Error: " & ex.Message)
            End Try
        End Using
    End Sub
    Private Sub btnsearchbooks_Click(sender As Object, e As EventArgs) Handles btnsearchbooks.Click
        ' SQL query to search Books based on input in txtsearchbooks
        Dim sqlSearch As String = "SELECT * FROM Books WHERE ISBN LIKE @search OR BookName LIKE @search"

        Using conn As New SqlConnection(connectionString)
            Dim cmd As New SqlCommand(sqlSearch, conn)
            ' Parameterize the query to avoid SQL injection
            cmd.Parameters.AddWithValue("@search", "%" & txtsearchbooks.Text & "%")

            Dim adapter As New SqlDataAdapter(cmd)
            Dim dt As New DataTable()

            Try
                conn.Open()
                adapter.Fill(dt)
                ' Bind the results to the DataGridView
                DataGridViewBooks.DataSource = dt
            Catch ex As Exception
                MessageBox.Show("Error: " & ex.Message)
            End Try
        End Using
    End Sub


    'Private Sub PNLLOGIN_Paint(sender As Object, e As PaintEventArgs) Handles PNLLOGIN.Paint

    'End Sub

    'Private Sub TXTUSERNAME_TextChanged(sender As Object, e As EventArgs) Handles TXTUSERNAME.TextChanged

    'End Sub



    ' Button to Update the selected row

End Class