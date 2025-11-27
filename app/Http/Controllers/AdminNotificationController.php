<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;

class AdminNotificationController extends Controller
{
    <?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use App\Models\User;
use Filament\Notifications\Notification;

class AdminNotificationController extends Controller
{
    // Show the form to create a notification
    public function create()
    {
        return view('admin.send-notification');
    }

    // Send the notification
    public function send(Request $request)
    {
        // Validate input
        $request->validate([
            'title' => 'required|string|max:255',
            'message' => 'required|string',
        ]);

        // Get all users with role 'user'
       $users = User::where('role', 'user')->get();
$admin = auth()->user(); // or User::where('role', 'admin')->first();

Notification::make()
    ->title($request->title)
    ->body($request->message)
    ->success()
    ->sendToDatabase($users);

// Send a separate notification to admin
Notification::make()
    ->title($request->title)
    ->body($request->message)
    ->success()
    ->sendToDatabase($admin);


        return redirect()->back()->with('success', 'Notification sent to all users!');
    }
}

}
