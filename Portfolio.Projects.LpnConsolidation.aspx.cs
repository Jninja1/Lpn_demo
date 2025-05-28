using System;
using System.Collections.Generic;
using System.Web;
using System.Data;
// Removed some sections to maintain security

using System.Web.Management;
using System.Diagnostics;
namespace Portfolio.Projects.LpnConsolidation
{
    public partial class LpnConsolidation : System.Web.UI.Page
    {
        private Session session;

        private ContainerInfo containerInfo1;
        private ContainerInfo containerInfo2;
        inveRequest inveRequest = new inveRequest();
        // Name changed to placeholder

        /// <summary>
        /// Loads the user's page and displays all relavent info
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"> e </param>
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                writeDebug("LpnMerge", "Page_Load");
                session = CreateSession.CreateSession();
                // Check and update info for item_1
                if (!string.IsNullOrEmpty(LPN1.Text) && (containerInfo1 == null || containerInfo1.containerId.ToUpper() != LPN1.Text.ToUpper()))
                {

                    containerInfo1 = new ContainerInfo(session, LPN1.Text);
                }

                // Check and update info for item_2
                if (!string.IsNullOrEmpty(LPN2.Text) && (containerInfo2 == null || containerInfo2.containerId.ToUpper() != LPN2.Text.ToUpper()))
                {

                    containerInfo2 = new ContainerInfo(session, LPN2.Text);
                }
            }
            catch (Exception error)
            {
                HandleException(error);
            }
        }

        // _________________________________________________________________________________________________________________________________
        private void clearMessage()
        {
            showMessage("");
        }

        // _________________________________________________________________________________________________________________________________

        private void showMessage(string e)
        {
            lblMessage.Text = e;

        }
        // _________________________________________________________________________________________________________________________________

        protected void bOkClicked(object sender, EventArgs e)
        {
            string result = string.Empty;
            bool success = false;

            try
            {
                clearMessage();

                // Makes sure the LPN is in a usable format
                if (string.IsNullOrEmpty(LPN1.Text) || !System.Text.RegularExpressions.Regex.IsMatch(LPN1.Text, @"^(?!-)[a-zA-Z0-9\-\@_]{1,50}$"))
                {
                    showMessage("Invalid Input for From LPN");
                    return;
                }
                // If the string is able to be passed through it gets sanitized before being sent out into the code
                string sanitized_fromLPN = HttpUtility.HtmlEncode(LPN1.Text);

                // Makes sure the LPN is in a usable format
                if (string.IsNullOrEmpty(LPN2.Text) || !System.Text.RegularExpressions.Regex.IsMatch(LPN2.Text, @"^(?!-)[a-zA-Z0-9\-\@_]{1,50}$"))
                {
                    showMessage("Invalid Input for To LPN");
                    return;
                }
                // If the string is able to be passed through it gets sanitized before being sent out into the code
                string sanitized_toLPN = HttpUtility.HtmlEncode(LPN2.Text);

                // Function call to process the logic of handling two LPNs. Depending on their eligibility different messages will be displayed

                result = LPN_logic(sanitized_fromLPN, sanitized_toLPN);
                if (result == "Transfer complete")
                {
                    success = true;
                }
                else
                {
                    success = false;
                }
                resetScreen(result, success);
            }

            catch (Exception error)
            {
                HandleException(error);
            }
        }
        // _________________________________________________________________________________________________________________________________

        public void resetScreen(string message, bool success)
        {
            lblMessage.Text = message;
            lblMessage.Visible = true;

            if (success)
            {
                // Inject JavaScript to clear the message after 1.5 seconds
                string script = @"
                                 <script type='text/javascript'>
                                    setTimeout(function() {
                                    document.getElementById('" + lblMessage.ClientID + @"').innerText = '';
                                    document.getElementById('" + LPN1.ClientID + @"').value = '';
                                    document.getElementById('" + LPN2.ClientID + @"').value = '';
                                }, 3000);
                            </script>";

                // Register the script to be executed on the client side
                ClientScript.RegisterStartupScript(this.GetType(), "ClearMessageScript", script);
            }
            else
            {
                // Inject JavaScript to clear the message after 1.5 seconds
                string script = @"
                                <script type='text/javascript'>
                                    setTimeout(function() {
                                        document.getElementById('" + lblMessage.ClientID + @"').innerText = '';
                                    }, 7000);
                                </script>";

                // Register the script to be executed on the client side
                ClientScript.RegisterStartupScript(this.GetType(), "ClearMessageScript", script);
            }

        }

        // _________________________________________________________________________________________________________________________________

        protected void bCancelClicked(object sender, EventArgs e)
        {
            try
            {
                Server.Transfer("SignonMenuRF.aspx");
            }
            catch (Exception error)
            {
                HandleException(error);
            }
        }
        // _______________________________________________    ***   LPN_logic BELOW  ****   ______________________________________________________


        /// <summary>
        /// LPN_logic is a class that connects other classes together to ensure that proper data is passed through to the inner logic.
        /// </summary>
        /// <param name="fromLPN">One of the two LPNs that are passed to the function. Normally this is the "fromLPN" from the aspx page</param>
        /// <param name="toLPN">One of the two LPNs that are passed to the fucntion. Normally this is the "toLPN" from the aspx page.</param>
        /// <returns></returns>
        public string LPN_logic(string fromLPN, string toLPN)
        /*
          Function to handle the logic of processeing the two provided LPNs. The SQL query can be changed to grab different data
            Normal flow would looks like being given two LPNs '1' and '2'.
            LPN '1' has the item of 'Apple' and locaiton of 'Foo'. LPN '2' has item of 'Apple' and location of 'Bar'
            Expected error is: "Locations do not match"
            LPN '3' is 'Apple' and 'Foo'. LPN '4' is 'Apple' and 'Foo'. Expected output is "Merge elligable".
        */
        {

            // Block to generate the two tables needed for initial comparison.
            GenerateLPNtable lpnTable = new GenerateLPNtable();
            string lpn_q = "SELECT unit, location, item, warehouse FROM loc WHERE unit = @LPN";
            DataTable fromLPN_data = lpnTable.GetSingleLPNData(session, lpn_q, fromLPN);
            DataTable toLPN_data = lpnTable.GetSingleLPNData(session, lpn_q, toLPN);

            string result = string.Empty;


            // If either of the tables are empty we then check which, or if both, table is empty
            if (fromLPN_data.Rows.Count == 0 || toLPN_data.Rows.Count == 0)
            {
                // Branch to give a better explanation of which LPN is null
                if (fromLPN_data.Rows.Count == 0 && toLPN_data.Rows.Count == 0)
                //Case if the both tables are empty.
                {
                    result = "From LPN does not exist.<br/> To LPN do not exist.";
                }
                else if (fromLPN_data.Rows.Count == 0)
                //If the first table is empty v
                {
                    result = "From LPN does not exist";
                }
                else
                // Second table is bad 
                {
                    result = "To LPN does not exist";
                }
            }
            else
            {
                SQL_logic sQL_Logic = new SQL_logic();
                string query = "SELECT unit, location, item, warehouse, stocked_qty, used_qty, " +
                    "sent_qty, stalled_qty, inv, lot " +
                    "FROM loc WHERE unit = @fromLPN OR unit = @toLPN";
                DataTable table = sQL_Logic.GetLPNData(session, query, fromLPN, toLPN);

                //resetScreen(result, success);
                if (table.Rows.Count > 1)
                {

                    // Grabs the location of the fromLPN, used for logic later on
                    string from_location;
                    if (table.Rows[1]["unit"].Equals(fromLPN))
                    {
                        from_location = (string)table.Rows[1]["LOCATION"];
                    }
                    else
                    {
                        from_location = (string)table.Rows[0]["LOCATION"];
                    }

                    // Grabs the location from the toLPN, used for logic later on
                    string to_location;
                    if (table.Rows[1]["unit"].Equals(toLPN))
                    {
                        to_location = (string)table.Rows[1]["LOCATION"];
                    }
                    else
                    {
                        to_location = (string)table.Rows[0]["LOCATION"];
                    }

                    // Grabs the item number from the fromLPN, used for logic later on
                    string from_item;
                    if (table.Rows[1]["unit"].Equals(fromLPN))
                    {
                        from_item = (string)table.Rows[1]["ITEM"];
                    }
                    else
                    {
                        from_item = (string)table.Rows[0]["ITEM"];
                    }

                    // Grabs the item number from the toLPN, used for logic later on
                    string to_item;
                    if (table.Rows[1]["unit"].Equals(toLPN))
                    {
                        to_item = (string)table.Rows[1]["ITEM"];
                    }
                    else
                    {
                        to_item = (string)table.Rows[0]["ITEM"];
                    }

                    // Grabs the name of the warehouse for both LPNs
                    string from_warehouse;
                    if (table.Rows[1]["unit"].Equals(fromLPN))
                    {
                        from_warehouse = (string)table.Rows[1]["warehouse"];
                    }
                    else
                    {
                        from_warehouse = (string)table.Rows[0]["warehouse"];
                    }

                    string to_warehouse;
                    if (table.Rows[1]["unit"].Equals(toLPN))
                    {
                        to_warehouse = (string)table.Rows[1]["warehouse"];
                    }
                    else
                    {
                        to_warehouse = (string)table.Rows[0]["warehouse"];
                    }

                    // Grabs the stocked_qty of the toLPN, no matter the order. This needs to be the toLPN qty for logic to work best.
                    //FIXME: Error in grabbing the data properly!
                    decimal to_stocked_qty;
                    if (table.Rows[1]["unit"].Equals(toLPN))
                    {
                        to_stocked_qty = (decimal)table.Rows[1]["stocked_qty"];
                    }
                    else
                    {
                        to_stocked_qty = (decimal)table.Rows[0]["stocked_qty"];
                    }

                    // Grabs the from_hand_qty of the fromLPN, no matter the order. This needs to be the toLPN qty for logic to work best.
                    decimal from_stocked_qty;
                    if (table.Rows[1]["unit"].Equals(fromLPN))
                    {
                        from_stocked_qty = (decimal)table.Rows[1]["stocked_qty"];
                    }
                    else
                    {
                        from_stocked_qty = (decimal)table.Rows[0]["stocked_qty"];
                    }


                    // Grabs the used_qty of the toLPN.
                    decimal from_used_qty;
                    if (table.Rows[1]["unit"].Equals(fromLPN))
                    {
                        from_used_qty = (decimal)table.Rows[1]["used_qty"];
                    }
                    else
                    {
                        from_used_qty = (decimal)table.Rows[0]["used_qty"];
                    }

                    // Grabs the used_qty of the fromLPN.
                    decimal to_used_qty;
                    if (table.Rows[1]["unit"].Equals(toLPN))
                    {
                        to_used_qty = (decimal)table.Rows[1]["used_qty"];
                    }
                    else
                    {
                        to_used_qty = (decimal)table.Rows[0]["used_qty"];
                    }

                    // Grabs the transit_qty of the toLPN.
                    decimal from_transit_qty;
                    if (table.Rows[1]["unit"].Equals(fromLPN))
                    {
                        from_transit_qty = (decimal)table.Rows[1]["sent_qty"];
                    }
                    else
                    {
                        from_transit_qty = (decimal)table.Rows[0]["sent_qty"];
                    }

                    // Grabs the transit_qty of the fromLPN.
                    decimal to_transit_qty;
                    if (table.Rows[1]["unit"].Equals(toLPN))
                    {
                        to_transit_qty = (decimal)table.Rows[1]["sent_qty"];
                    }
                    else
                    {
                        to_transit_qty = (decimal)table.Rows[0]["sent_qty"];
                    }

                    // Grabs thestalled_qty of the toLPN.
                    decimal from_suspend_qty;
                    if (table.Rows[1]["unit"].Equals(fromLPN))
                    {
                        from_suspend_qty = (decimal)table.Rows[1]["stalled_qty"];
                    }
                    else
                    {
                        from_suspend_qty = (decimal)table.Rows[0]["stalled_qty"];
                    }

                    // Grabs thestalled_qty of the fromLPN.
                    decimal to_suspend_qty;
                    if (table.Rows[1]["unit"].Equals(toLPN))
                    {
                        to_suspend_qty = (decimal)table.Rows[1]["stalled_qty"];
                    }
                    else
                    {
                        to_suspend_qty = (decimal)table.Rows[0]["stalled_qty"];
                    }

                    //Grabs the inventory status of the fromLPN
                    string from_status = table.Rows[0]["inv"].ToString();

                    //Grabs the inventory status of the toLPN
                    string to_status = table.Rows[1]["inv"].ToString();

                    // Grabs the lot from the fromLPN
                    string lot = table.Rows[0]["lot"].ToString();

                    if (from_warehouse == to_warehouse)
                    {
                        if (from_item == to_item)
                        {

                            // If each item matches we can then move to the next phase




                            if ((to_stocked_qty > 0) && (from_stocked_qty > 0) && (to_used_qty >= 0) &&
                                (from_used_qty == 0) && (to_transit_qty >= 0) && (from_transit_qty == 0) &&
                                (to_suspend_qty >= 0) && (from_suspend_qty == 0) && (from_status == to_status))
                            // One final check to see if the qty values are appropriate and if the status matches
                            {

                                InventoryTransfer transfer = new InventoryTransfer(session, from_warehouse, from_item, lot, from_location, to_location, to_stocked_qty, fromLPN, toLPN);

                                // Call the Execute method on the instance
                                transfer.Execute();
                                result = "Transfer complete";
                            }
                            else
                            {
                                //____________________________________________________________________________________________________________________________________

                                // Error messages for if the stocked_qty is less than 0
                                if ((to_stocked_qty <= 0) || (from_stocked_qty <= 0))
                                {
                                    if ((to_stocked_qty <= 0) && (from_stocked_qty <= 0))
                                    {
                                        result += "On hand Quantity for both LPNs are zero or less.<br/> ";
                                    }
                                    else if (to_stocked_qty <= 0)
                                    {
                                        result += "toLPN's on hand quantity is zero or less.<br/> ";
                                    }
                                    else
                                    {
                                        result += "fromLPN's on hand quantity is zero or less.<br/> ";
                                    }
                                }

                                // Error messages if the used_qty is not 0
                                if (from_used_qty != 0)
                                {
                                    result += "fromLPN has inventory allocated.<br/>";
                                }

                                //Error message if the transit_qty is is not zero
                                if (from_transit_qty != 0)
                                {
                                    result += "fromLPN has inventory in transit.<br/>";
                                }

                                //Error messages if the suspended_qty is less than 0
                                if (from_suspend_qty != 0)
                                {
                                    result += "fromLPN has suspended quantity.<br/>";
                                }

                                if (from_status != to_status)
                                {
                                    result += string.Format("The status for both LPNs do not match. <br/>fromLPN status: {0}<br/>toLPN status: {1}", from_status, to_status);

                                }
                            }
                            //______________________________________________________________________________________________-_____________________________________

                        }
                        else
                        {
                            // If the item number doesn not match, this line is proked
                            result = string.Format("Items do not match<br/>fromLPN item: {0}<br/>toLPN item:{1}", from_item, to_item);
                            // Honestly this one is super common, most of the LPNs don't have matching items 
                        }
                    }
                    else
                    {
                        result = string.Format("The warehouses do not match.<br/>fromLPN warehouse: {0}<br/>toLPN warehouse: {1}", from_warehouse, to_warehouse);
                    }
                }
                else
                {
                    result = "No locations found for the given LPNs.";
                }
            }

            // Returns the result string to be printed.
            return result;
        }

        // __________________________________________________  ***GenerateLPNtable BELOW***    __________________________________________________________________

        /// <summary>
        /// GenerateLPNtable does what it sounds like. It generates data fro a single LPN. The table is used for simple comparison.
        /// </summary>
        public class GenerateLPNtable
        {
            public GenerateLPNtable()
            {
            }

            /// <summary>
            /// GetSingleLPNData returns a table of relevant data based off of the given query and useses LPN as a var.
            /// </summary>
            /// <param name="session">Default session that is defined at the top of the namespace</param>
            /// <param name="query">Query can be anything to grab data. Best to incldue a variable for the LPN. Such as select * ... where x = LPN</param>
            /// <param name="LPN">The provided LPN. Normally a string of letters and numbers</param>
            /// <returns>Returns a table containing the data generated from the sql</returns>
            /// <exception cref="Exception">Thrown if the table generated is null</exception>
            public DataTable GetSingleLPNData(Session session, string query, string LPN)
            {
                DataTable table = null;

                try
                {
                    using (DataHelper dataHelper = new DataHelper(session))
                    {
                        table = dataHelper.GetTable(CommandType.Text, query,
                            dataHelper.BuildParameter("@LPN", LPN));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred while executing the SQL query: " + ex.Message);
                }
                return table;
            }
        }

        // __________________________________________________   ***  SQL_logic BELOW   ***   ____________________________________________________________________________

        /// <summary>
        /// SQL_logic is responsible for handling the operation of SQL for the LPNs
        /// </summary>
        public class SQL_logic
        /*
            Takes two LPNs to confirm both have the same location, they're both the saem item, AND they exist
            If any check fails the whole operation should terminate as the merge should not be permitted
        */
        {
            public SQL_logic()
            {
            }

            /// <summary>
            /// Generate a table based off of the given LPNs and query.
            /// </summary>
            /// <param name="session"> The session used to allow access to the DB server</param>
            /// <param name="query"> The custom query used for obtaining data. Works best if both LPNs are used. Such as 
            /// "select * from x where a = LPN1 and a = LPN2" </param>
            /// <param name="fromLPN"> The first, fromLPN, that is handed to the function. These are normally letters and numbers </param>
            /// <param name="toLPN"> The second, toLPN, that is handed to the function. These are normally letters and numbers </param>
            /// <returns> Returns a data table</returns>
            /// <exception cref="Exception"> If the table is unable to be made an error is called. </exception>
            public DataTable GetLPNData(Session session, string query, string fromLPN, string toLPN)


            {
                DataTable table;

                // Local vars that will be used to access data later!

                try
                {
                    using (DataHelper dataHelper = new DataHelper(session))
                    //Establishes the connection
                    {
                        table = dataHelper.GetTable(CommandType.Text, query,
                            dataHelper.BuildParameter("@fromLPN", fromLPN),
                            dataHelper.BuildParameter("@toLPN", toLPN));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred while executing the SQL query: " + ex.Message);
                }
                return table;
            }
        }

        // _________________________________________________________________________________________________________________________________

        private void HandleException(Exception e)
        {
            showMessage(e.Message);
            ExceptionManager.LogException(session, e);
        }

        // _________________________________________________________________________________________________________________________________


        private void writeDebug(string Note, string Message)
        {
            //Note sure why this is not working.
            System.Diagnostics.Trace.Write(string.Format("E: {0}: {1}", Note, Message));

            if (false)
                lblDebug.Text = string.Format("{0}{1}{2}: {3}", lblDebug.Text, Environment.NewLine, Note, Message);
        }

        // _________________________________________________________________________________________________________________________________

        public class ContainerInfo
        {
            public string containerId;
            public string ToLoc;
            public string CheckDigit;
            public string Error;

            private Session session;

            public ContainerInfo(Session session, string containerId)
            {
                this.containerId = containerId;
            }


        }

        // ___________________________________________    ***  InventoryTransfer BELOW ***    _____________________________________________________________

        /// <summary>
        /// Class responsible for, you guessed it, inventory transfers
        /// </summary>

        public class InventoryTransfer
        {
            invReq invReq = new invReq();
            Session session;

            /// <summary>
            /// This function works on transfering all data from the 'fromLPN' to be the 'toLPN'. Once merged the qty is added together and the fromLPN is removed from the DB
            /// </summary>
            /// <param name="session"> Session that is defined at the top of the name space. </param>
            /// All vars below are handed from the LPN_logic function
            /// <param name="warehouse"> The given warehouse of both LPNs. In our case that's PPLANT </param>
            /// <param name="item"> The serial number of the item. </param>
            /// <param name="lot"> The lot of the given LPNs, by default this is null </param>
            /// <param name="fromLoc"> The location of the fromLPN. </param>
            /// <param name="toLoc"> The location of the toLPN. </param>
            /// <param name="qty"> The stocked_qty of the toLPN</param>
            /// <param name="fromlpn"> The fromLPN. Just the logistics number</param>
            /// <param name="tolpn"> The toLPN. Just the logistics number</param>
            public InventoryTransfer(Session session, string warehouse, string item, string lot, string fromLoc, string toLoc, decimal qty, string fromlpn, string tolpn)
            {
                this.session = session;
                DataRow row;

                // Stored procedure inside of test server. Grabs all of the data outlined below it
                string sp = "Transfer_LPN";

                using (DataHelper helper = new DataHelper(session))
                {
                    DataTable table = helper.GetTable(CommandType.StoredProcedure, sp,
                        helper.BuildParameter("@warehouse", warehouse),
                        helper.BuildParameter("@item", item),
                        helper.BuildParameter("@lot", lot),
                        helper.BuildParameter("@fromLoc", fromLoc),
                        helper.BuildParameter("@toLoc", toLoc),
                        helper.BuildParameter("@Qty", qty)
                    );

                    if (table.Rows.Count == 0)
                    {
                        throw ErrorDebuger.BuildException(
                         string.Format("Failed to find inventory to transfer from {1}. Check that sufficient quantity existed to complete the request. The store procedure named {0} did not return results.", sp, fromLoc),
                            new List<string>
                            {
                        string.Format("warehouse = {0}", warehouse),
                        string.Format("Item = {0}", item),
                        string.Format("lot = {0}", lot),
                        string.Format("fromLoc = {0}", fromLoc),
                        string.Format("toLoc = {0}", toLoc),
                        string.Format("qty = {0}", qty)
                            });
                    }



                    row = table.Rows[0];
                }

                ErrorDebuger.write(
                    "invTransfer",
                    string.Format("DataManager.GetString(row, \"inv\")={0}.  DataManager.GetString(row, \"ITEM_DESC\")={1}.  DataManager.GetString(row, \"QUANTITY_UM\")={2}",
                    DataManager.GetString(row, "inv"),
                    DataManager.GetString(row, "ITEM_DESC"),
                    DataManager.GetString(row, "QUANTITY_UM"))
                );


                SetValues(
                    item: item,
                    fromLoc: fromLoc,
                    toLoc: toLoc,
                    quantity: decimal.ToDouble(qty),
                    lot: lot,
                    expirationDate: null,
                    lpn: fromlpn,
                    warehouse: warehouse,
                    inventorySts: DataManager.GetString(row, "inv"),
                    beforeInventorySts: DataManager.GetString(row, "inv"),
                    adjustmentType: "Transfer",
                    referenceType: "LPN Transfer",
                    company: DataManager.GetString(row, "COMPANY"),
                    ItemDescription: DataManager.GetString(row, "ITEM_DESC"),
                    qtyUm: DataManager.GetString(row, "QUANTITY_UM"),
                    FromLpn: fromlpn,
                    ToLpn: tolpn
                );


            }

            // _________________________________________________________________________________________________________________________________

            public void Execute()
            {
                //ErrorDebuger.write("InventoryTransfer.Execute", "v1");
                Transfer(SessionMapper.ConvertToLegacySession(session).transToString());
            }

            // _____________________________________________    ***   Transfer BELOW ***   ________________________________________________________________________


            /// <summary>
            /// Function to call the transfer
            /// </summary>
            /// <param name="sessString"> The session required to complete the trasnfer. </param>
            private void Transfer(string sessString)
            {
                ErrorDebuger.write("InventoryTransfer.Transfer", "executing transfer with the following invReq values");

                /*
                 // Just a debug statment to check if the proper data is being passed through

                 throw ErrorDebuger.BuildException(
                    string.Format    (
                    "getItem() = {0}<br/>, getFromLoc() = {1}<br/>, getToLoc() = {2}<br/>, getQuantity() = {3}<br/>, getLot() = {4}<br/>, getExpDate() = {5}<br/>, getContainerID() = {6}<br/>, GetFromLocInvAttributeId() = {7}<br/>, GetToLocInvAttributeId() = {8}<br/>, getPickedFullContainers() = {9}<br/>, getUserDef7() = {10}<br/>, getUserDef8() = {11}<br/>, getTransType() = {12}<br/>, getToWhs() = {13}<br/>, getFromWhs() = {14}<br/>, getInventorySts() = {15}<br/>, getBeforeInventorySts() = {16}<br/>, getAdjustmentType() = {17}<br/>, getCompany() = {18}<br/>, getItemDesc() = {19}<br/>, getQtyUM() = {20}",
                        invReq.getItem(), invReq.getFromLoc(), 
                        invReq.getToLoc(), invReq.getQuantity(), invReq.getLot(), 
                        invReq.getExpDate(), invReq.getContainerID(), invReq.GetFromLocInvAttributeId(), 
                        invReq.GetToLocInvAttributeId(), invReq.getPickedFullContainers(), 
                        invReq.getUserDef7(), invReq.getUserDef8(), invReq.getTransType(), 
                        invReq.getToWhs(), invReq.getFromWhs(), invReq.getInventorySts(), 
                        invReq.getBeforeInventorySts(), invReq.getAdjustmentType(), invReq.getCompany(),
                        invReq.getItemDesc(), invReq.getQtyUM()
                    ),
                    new List<string>
                    {

                    }
                );
                */


                string error = InventoryHandling.transferInv(sessString, invReq.translateToString(), false, true);
                if (!string.IsNullOrEmpty(error))
                {
                    ErrorDebuger.BuildException(
                        string.Format("InventoryTransfer.Transfer InventoryHandling.transferInv returned the value: {0}", error),
                    new List<string> { invReq.translateToString() }
                    );

                    string errorCode = "";
                    try
                    {
                        errorCode = Manager.GetStringResource(session, error, ResourceGroups.Msg);
                    }
                    catch { }

                    throw ErrorDebuger.BuildException(
                        string.Format("Error transferring inventory. Scale returned {0}: {1}<br/>From qty: {2}", error, errorCode, invReq.getQuantity()),
                        new List<string> { invReq.translateToString() }
                    );

                }

            }

            // ________________________________________________   *** SetValues BELOW  *** __________________________________________________________________________

            /// <summary>
            /// Sets all of the given vars for the transfer
            /// </summary>
            /// <param name="item"> Grabs the serial number from the item that is stored in the DB. </param>
            /// <param name="fromLoc"> Grabs the location from the fromLPN that is stored in the DB. </param>
            /// <param name="toLoc"> Grabs the location from the toLPN that is stored in the DB. </param>
            /// <param name="quantity"> Grabs the qty from the item that is stored in the DB. </param>
            /// <param name="lot"> Grabs the lot from the item that is stored in the DB. This is null by default. </param>
            /// <param name="expirationDate"> Grabs the experation date from the item that is stored in the DB. By default this is null</param>
            /// <param name="lpn"> Grabs the LPN from the item that is stored in the DB. </param>
            /// <param name="warehouse"> Grabs the warehouse from the item that is stored in the DB. </param>
            /// <param name="inventorySts"> Grabs the inventory status from the item that is stored in the DB. </param>
            /// <param name="beforeInventorySts"> Grabs the before inventory status from the item that is stored in the DB. </param>
            /// <param name="adjustmentType"> Grabs the adjustment type from the item that is stored in the DB. </param>
            /// <param name="company"> Grabs the company from the item that is stored in the DB. By default this is null </param>
            /// <param name="ItemDescription"> Grabs the item's desc from the item that is stored in the DB. </param>
            /// <param name="qtyUm"> Grabs the qtyUm from the item that is stored in the DB. </param>
            /// <param name="FromLpn"> Grabs the fromLPN's number from DB. </param>
            /// <param name="ToLpn"> Grabs the toLPN's number from the DB. </param>
            private void SetValues(string item, string fromLoc, string toLoc, double quantity, string lot, string expirationDate, string lpn, string warehouse, string inventorySts, string beforeInventorySts, string adjustmentType, string referenceType, string company, string ItemDescription, string qtyUm, string FromLpn, string ToLpn)
            {
                invReq.setItem(item);
                invReq.setFromLoc(fromLoc);
                invReq.setToLoc(toLoc);
                invReq.setQuantity(quantity);

                if (!string.IsNullOrEmpty(lot))
                {
                    invReq.setLot(lot);
                }
                else if (string.IsNullOrEmpty(lot))
                {
                    invReq.setLot(null);
                }

                if (!string.IsNullOrEmpty(expirationDate))
                {
                    invReq.setExpDate(expirationDate);
                }
                else if (string.IsNullOrEmpty(expirationDate))
                {
                    invReq.setExpDate(null);
                }

                invReq.setContainerID(string.IsNullOrEmpty(lpn) ? null : lpn);


                invReq.SetFromLocInvAttributeId(0);
                invReq.SetToLocInvAttributeId(0);
                invReq.setPickedFullContainers(false);
                invReq.setUserDef7(0);
                invReq.setUserDef8(0);
                invReq.setTransType("60");
                invReq.setToWhs(warehouse);
                invReq.setFromWhs(warehouse);
                invReq.setInventorySts(inventorySts);
                invReq.setBeforeInventorySts(beforeInventorySts);
                invReq.setAdjustmentType(adjustmentType);
                invReq.setReferenceType(referenceType);

                if (!string.IsNullOrEmpty(company))
                {
                    invReq.setCompany(company);
                }
                else if (string.IsNullOrEmpty(company))
                {
                    invReq.setCompany(null);
                }
                invReq.setItemDesc(ItemDescription);
                invReq.setQtyUM(qtyUm);

                if (!string.IsNullOrEmpty(lpn))
                {
                    ErrorDebuger.write("InventoryTransfer.Transfer", string.Format("if (!string.IsNullOrEmpty(lpn))=true lpn={0}, quantity={1}, ToLpn={2}", lpn, quantity, ToLpn));
                    invReq.setFromContainerID(new string[1] { lpn });
                    invReq.setFromContQty(new double[1] { quantity });
                    invReq.setToContainerID(new string[1] { ToLpn });
                }
            }
        }
    }
}
