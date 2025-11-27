<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class SDGStatus extends Model
{
    use HasFactory;

    protected $table = 'sdg_status';

    protected $fillable = [
        'sdg3',
        'sdg6',
        'sdg7',
        'sdg8',
        'sdg9',
        'sdg11',
        'sdg12',
        'sdg13',
        'sdg15',
        'status',
        'project_id',
    ];

    public function project()
    {
        return $this->belongsTo(Project::class);
    }
}
