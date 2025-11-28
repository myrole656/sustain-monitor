<?php

use App\Http\Controllers\Auth\RegisterController;
use App\Http\Controllers\Auth\RoleLoginController;
use Illuminate\Support\Facades\Route;
use App\Http\Controllers\PDFController;

use Filament\Notifications\Notification;
use App\Http\Controllers\AdminNotificationController;
use Filament\Notifications\Actions\Action;

use Prism\Prism\Facades\Prism;
use Prism\Prism\Enums\Provider;




// Home route
Route::get('/', function () {
    return redirect()->route('login');
})->name('home');

Route::get('/test', function () {
 
      $response = Prism::text()
    ->using(Provider::Gemini, 'gemini-2.0-flash')
    ->withPrompt('definition of gey') 
    ->asText();
    dd($response->text);
})->name('test');

// Login
Route::get('/login', [RoleLoginController::class, 'showLoginForm'])->name('login');
Route::post('/login', [RoleLoginController::class, 'login'])->name('login.submit');
Route::post('/logout', [RoleLoginController::class, 'logout'])->name('logout');


// Registration
Route::get('/register/admin', [RegisterController::class, 'showAdminForm'])->name('register.admin.page');
Route::post('/register/admin', [RegisterController::class, 'registerAdmin'])->name('register.admin.submit');

Route::get('/register/user', [RegisterController::class, 'showUserForm'])->name('register.user.page');
Route::post('/register/user', [RegisterController::class, 'registerUser'])->name('register.user.submit');


// Admin-only
Route::middleware(['auth', 'admin'])->group(function () {
    Route::get('/admin/dashboard', function () {
        return view('admin.dashboard');
    })->name('admin.dashboard');
});


// PDF Project Route
Route::get('/pdf/project/{id}', [PDFController::class, 'projectReport'])
    ->name('pdf.project');

    Route::middleware(['auth', 'admin'])->group(function () {
    Route::get('/admin/notifications/create', [AdminNotificationController::class, 'create'])->name('admin.notifications.create');
    Route::post('/admin/notifications/send', [AdminNotificationController::class, 'send'])->name('admin.notifications.send');
});





